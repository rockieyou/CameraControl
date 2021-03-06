﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ThridLibray;
using System.IO;


namespace SoftwareTrigger
{
    public partial class frmMain : Form
    {
        List<IGrabbedRawData> m_frameList = new List<IGrabbedRawData>();        /* 图像缓存列表 */
        Thread renderThread = null;         /* 显示线程  */
        bool m_bShowLoop = true;            /* 线程控制变量 */
        Mutex m_mutex = new Mutex();        /* 锁，保证多线程安全 */
        private Graphics _g = null;
        bool m_bShowByGDI;                  /* 是否使用GDI绘图 */
        private Bitmap bitmap = null;       /* 临时采集的帧图像 */
        private String strSavePath;    // 图片保存的总目录
        private String strSavePathOK;  // OK文件的保存路径
        private String strSavePathNG;  // NG文件的保存路径
        private Int32 nExpTime;  // 曝光时间
        private float fCamGain;  // 相机增益 
        private bool bVideoMode; // 是否为录像模式

        private HDevelopExport detectInstanceDyn;
        private HDevelopExport1 detectInstanceNorm;
        private ConfigInfo myConfig; // 检测参数配置类

        private String strFileName;
        private Image imgOri;
        // private Bitmap imgDone = null;

        public frmMain()
        {
            InitializeComponent();
            myConfig = new ConfigInfo();
            Pro.SelectedObject = myConfig;
            Pro.PropertyValueChanged += Pro_PropertyValueChanged;
            nExpTime = 2000;
            fCamGain = 1.1f;
            bVideoMode = false;
            strSavePath = "";
            strSavePathOK = "";
            strSavePathNG = "";
            strFileName = "";
            imgOri = null;
            detectInstanceDyn = null;
            detectInstanceNorm = null;
        }

        /* 设置图片保存路径 */
        public void SetSavePath(String strPath)
        {
            strSavePath = strPath + "\\";
        }
        
        /* 设置曝光时间 */
        public void SetExpTime(Int32 nTime)
        {
            nExpTime = nTime;
        }

        /* 设置相机增益 */
        public void SetCamGain(float fGain)
        {
            fCamGain = fGain;
        }

        /* 检查路径是否存在，如果不存在则创建该路径 */
        private void CheckAndCreatePath(String strPath)
        {
            if (!Directory.Exists(strPath))
            {
                if (strPath.Length != 0)
                {
                    Directory.CreateDirectory(strPath);
                }
            }
        }

        /* 当界面检测参数改变时，数据写回config变量 */
        private void Pro_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            myConfig = Pro.SelectedObject as ConfigInfo;
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            /* 初始化设备关闭按钮 */
            btnClose.Enabled = false;

            if (null == renderThread)
            {
                renderThread = new Thread(new ThreadStart(ShowThread));
                renderThread.Start();
            }
            m_stopWatch.Start();
        }

        /* 转码显示线程 */
        private void ShowThread()
        {
            while (m_bShowLoop)
            {
                if (m_frameList.Count == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }

                /* 图像队列取最新帧 */
                m_mutex.WaitOne();
                IGrabbedRawData frame = m_frameList.ElementAt(m_frameList.Count - 1);
                m_frameList.Clear();
                m_mutex.ReleaseMutex();

                /* 主动调用回收垃圾 */
                GC.Collect();

                /* 控制显示最高帧率为25FPS */
                if (false == isTimeToDisplay())
                {
                    continue;
                }

                try
                {
                    /* 图像转码成bitmap图像 */
                    bitmap = frame.ToBitmap(false);
                    m_bShowByGDI = true;
                    if (m_bShowByGDI)
                    {
                        /* 使用GDI绘图 */
                        if (_g == null)
                        {
                            _g = pbImage.CreateGraphics();
                        }
                        _g.DrawImage(bitmap, new Rectangle(0, 0, pbImage.Width, pbImage.Height),
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
                        if (bitmap != null)
                        {
                            imgOri = bitmap;
                        }
                        // bitmap.Save(strSavePath + strFileName);
                        // bitmap.Dispose();
                    }
                    else
                    {
                        /* 使用控件绘图,会造成主界面卡顿 */
                        if (InvokeRequired)
                        {
                            BeginInvoke(new MethodInvoker(() =>
                            {
                                try
                                {
                                    pbImage.Image = bitmap;
                                }
                                // save captured image
                                // bitmap.Save("C:\\Users\\rocki\\Desktop\\captured\\shot.jpg");

                                catch (Exception exception)
                                {
                                    Catcher.Show(exception);
                                }
                            }));
                        }
                    }
                }
                catch (Exception exception)
                {
                    Catcher.Show(exception);
                }
            }
        }

        const int DEFAULT_INTERVAL = 40;
        Stopwatch m_stopWatch = new Stopwatch();    /* 时间统计器 */

        /* 判断是否应该做显示操作 */
        private bool isTimeToDisplay()
        {
            m_stopWatch.Stop();
            long m_lDisplayInterval = m_stopWatch.ElapsedMilliseconds;
            if (m_lDisplayInterval <= DEFAULT_INTERVAL)
            {
                m_stopWatch.Start();
                return false;
            }
            else
            {
                m_stopWatch.Reset();
                m_stopWatch.Start();
                return true;
            }
        }

        /* 设备对象 */
        private IDevice m_dev;

        /* 相机打开回调 */
        private void OnCameraOpen(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                btnOpen.Enabled = false;
                btnOpen.BackColor = btnClose.BackColor;
                btnClose.Enabled = true;
                btnSoftwareTrigger.Enabled = true;
            }));
        }

        /* 相机关闭回调 */
        private void OnCameraClose(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                btnOpen.Enabled = true;
                btnOpen.BackColor = btnDetect.BackColor;
                btnClose.Enabled = false;
                btnSoftwareTrigger.Enabled = false;
            }));
        }

        /* 相机丢失回调 */
        private void OnConnectLoss(object sender, EventArgs e)
        {
            m_dev.ShutdownGrab();
            m_dev.Dispose();
            m_dev = null;

            this.Invoke(new Action(() =>
            {
                btnOpen.Enabled = true;
                btnClose.Enabled = false;
                btnSoftwareTrigger.Enabled = false;
            }));
        }

        /* 码流数据回调 */
        private void OnImageGrabbed(Object sender, GrabbedEventArgs e)
        {
            m_mutex.WaitOne();
            m_frameList.Add(e.GrabResult.Clone());
            m_mutex.ReleaseMutex();           
        }

        /* 停止码流 */
        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_dev == null)
                {
                    throw new InvalidOperationException("Device is invalid");
                }

                m_dev.StreamGrabber.ImageGrabbed -= OnImageGrabbed;         /* 反注册回调 */
                m_dev.ShutdownGrab();                                       /* 停止码流 */
                m_dev.Close();                                              /* 关闭相机 */
            }
            catch (Exception exception)
            {
                Catcher.Show(exception);
            }
        }

        /* 窗口关闭 */
        protected override void OnClosed(EventArgs e)
        {
            if (m_dev != null)
            {
                m_dev.Dispose();
                m_dev = null;
            }

            m_bShowLoop = false;
            renderThread.Join();
            if (_g != null)
            {
                _g.Dispose();
                _g = null;
            }
            base.OnClosed(e);
        }

        /* 打开相机按钮事件 */
        private void btnOpen_Click(object sender, EventArgs e)
        {
            try
            {
                /* 设备搜索 */
                List<IDeviceInfo> li = Enumerator.EnumerateDevices();
                if (li.Count > 0)
                {
                    /* 获取搜索到的第一个设备 */
                    m_dev = Enumerator.GetDeviceByIndex(0);

                    /* 注册链接时间 */
                    m_dev.CameraOpened += OnCameraOpen;
                    m_dev.ConnectionLost += OnConnectLoss;
                    m_dev.CameraClosed += OnCameraClose;

                    /* 打开设备 */
                    if (!m_dev.Open())
                    {
                        MessageBox.Show(@"连接相机失败");
                        return;
                    }

                    /* 打开Software Trigger */
                    if (bVideoMode)
                    {
                        m_dev.TriggerSet.Close();
                    }
                    else
                    {
                        m_dev.TriggerSet.Open(TriggerSourceEnum.Software);
                    }

                    /* 设置图像格式 */
                    using (IEnumParameter p = m_dev.ParameterCollection[ParametrizeNameSet.ImagePixelFormat])
                    {
                        p.SetValue("Mono8");
                    }

                    /* 设置曝光 */
                    using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.ExposureTime])
                    {
                        p.SetValue(nExpTime);
                    }

                    /* 设置增益 */
                    using (IFloatParameter p = m_dev.ParameterCollection[ParametrizeNameSet.GainRaw])
                    {
                        p.SetValue(fCamGain);
                    }

                    /* 设置缓存个数为8（默认值为16） */
                    m_dev.StreamGrabber.SetBufferCount(8);

                    /* 注册码流回调事件 */
                    m_dev.StreamGrabber.ImageGrabbed += OnImageGrabbed;

                    /* 开启码流 */
                    if (!m_dev.GrabUsingGrabLoopThread())
                    {
                        MessageBox.Show(@"开启码流失败");
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                Catcher.Show(exception);
            }
        }
        /* 执行软触发 */
        private void btnSoftwareTrigger_Click(object sender, EventArgs e)
        {
            if (m_dev == null)
            {
                throw new InvalidOperationException("Device is invalid");
            }

            try
            {
                if (bVideoMode)
                {
                    m_dev.TriggerSet.Close();
                }
                else
                {
                    m_dev.ExecuteSoftwareTrigger();
                }     

                String strOldFilePath = strSavePath + strFileName;
                if (File.Exists(strOldFilePath))
                {
                    File.Delete(strOldFilePath); // delete the last frame image
                }

                System.DateTime currentTime = new System.DateTime();
                currentTime = System.DateTime.Now;
                strFileName = currentTime.ToString("yyyy-MM-dd HH:mm:ss").Replace(" ", "_").Replace(":", "-") + ".png"; 
                // strFileName = "test.jpg";
                String strFullPath = strSavePath + strFileName;

                // openFileDialog1.Filter = "BMP文件|*.bmp|JPEG文件|*.jpg";

                if (strFullPath != "") // openFileDialog1.ShowDialog() == DialogResult.OK
                {
                    // imgOri = Image.FromFile(strFullPath);
                    // pbImage.Image = imgOri;
                    hWindowCtrl.Visible = false;
                    pbImage.Visible = true;
                    lblResult.Text = "UN";
                    lblResult.BackColor = Color.White;
                }
            }
            catch (Exception exception)
            {
                Catcher.Show(exception);
            }
        }

        /* 检测按钮事件 */
        private void btnDetect_Click(object sender, EventArgs e)
        {
            pbImage.Visible = false;
            hWindowCtrl.Visible = true;
            if (imgOri != null)
            {
                Int32 nDefects = 0;
                Int32 nMode = 1;
                String strFullFileName;

                if (strSavePath == "")
                {
                    strSavePath = Application.StartupPath;
                }

                strFullFileName = strSavePath + strFileName;

                // save temp image 

                imgOri.Save(strFullFileName);

                switch (nMode)
                {
                    case 0:
                        // method 0: dynamic thresholding
                        detectInstanceDyn = new HDevelopExport();
                        detectInstanceDyn.InitHalcon();
                        if (File.Exists(strFullFileName))
                        {
                            detectInstanceDyn.RunHalcon(hWindowCtrl.HalconWindow, strFullFileName, myConfig.面积, myConfig.滤波尺寸, myConfig.动态二值化阈值, out nDefects);
                        }
                        break;
                    case 1:
                        // method 1: normal thresholding
                        detectInstanceNorm = new HDevelopExport1();
                        detectInstanceNorm.InitHalcon();
                        if (File.Exists(strFullFileName))
                        {
                            detectInstanceNorm.RunHalcon(hWindowCtrl.HalconWindow, strFullFileName, myConfig.面积, myConfig.灰度值, out nDefects);
                        }
                        break;
                }

                if (nDefects > 0)
                {
                    lblResult.Text = "NG";
                    lblResult.BackColor = Color.Red;
                    if (strSavePath != null && strSavePath.Length != 0)
                    {
                        strSavePathNG = strSavePath + "\\NG";
                        CheckAndCreatePath(strSavePathNG);
                        if (pbImage.Image != null)
                        {
                            pbImage.Image.Save(strSavePathNG + "\\" + strFileName);
                        }
                    }
                }
                else
                {
                    lblResult.Text = "OK";
                    lblResult.BackColor = Color.Green;
                    if (strSavePath != null && strSavePath.Length != 0)
                    {
                        strSavePathOK = strSavePath + "\\OK";
                        CheckAndCreatePath(strSavePathOK);
                        if (pbImage.Image != null)
                        {
                            pbImage.Image.Save(strSavePathOK + "\\" + strFileName);
                        }
                    }
                }
            }

            pbImage.Image = imgOri;
        }

        /* 显示原图按钮事件 */
        private void btnShowImg_Click(object sender, EventArgs e)
        {
            // hWindowCtrl.BackgroundImage = pbImage.Image;

            hWindowCtrl.Visible = false;
            pbImage.Visible = true;
            // pbImage.Image = imgOri;
        }

        /* 单击设置菜单事件 */
        private void ToolStripSet_Click(object sender, EventArgs e)
        {
            FormConfig frmCfg = new FormConfig(this, null);
            frmCfg.Show();
        }

        /* 工作模式改变事件 */
        private void chkboxMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkboxMode.Checked)
            {
                bVideoMode = true;
            }
            else
            {
                bVideoMode = false;
            }

            if (m_dev != null)
            {
                if (bVideoMode)
                {
                    m_dev.TriggerSet.Close();
                }
                else
                {
                    m_dev.TriggerSet.Open(TriggerSourceEnum.Software);
                }
            }
        }
    }
}
