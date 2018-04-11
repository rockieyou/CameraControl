//
//  File generated by HDevelop for HALCON/DOTNET (C#) Version 8.0
//
//  This file is intended to be used with the project HDevelopTemplate
//  which can be found in %HALCONROOT%\examples\c#\HDevelopTemplate\
//

using System;
using HalconDotNet;
using System.Drawing;
using System.Drawing.Imaging;

public class HDevelopExport1
{
    private HTuple hv_ExpDefaultWinHandle;

    // Main procedure 
    private void action(String strFileName, Int32 nAreaMin, Int32 nThreshLow, out Int32 nDefectNum)
    {
        // Local iconic variables
        HObject ho_Image, ho_Region, ho_RegionFillUp;
        HObject ho_ImageReduced, ho_ConnectedRegions, ho_SelectedRegions;
        HObject ho_Region1;

        // Local control variables 
        HTuple hv_Number, hv_Width, hv_Height, pointer, type;

        // Initialize local and output iconic variables 
        HOperatorSet.GenEmptyObj(out ho_Image);
        HOperatorSet.GenEmptyObj(out ho_Region);
        HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
        HOperatorSet.GenEmptyObj(out ho_ImageReduced);
        HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
        HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
        HOperatorSet.GenEmptyObj(out ho_Region1);

        ho_Image.Dispose();
        HOperatorSet.ReadImage(out ho_Image, strFileName);
        HOperatorSet.GetImagePointer1(ho_Image, out pointer, out type, out hv_Width, out hv_Height);
        HOperatorSet.SetPart(hv_ExpDefaultWinHandle, 0, 0, hv_Height - 1, hv_Width - 1);
        HOperatorSet.SetColor(hv_ExpDefaultWinHandle, "red");
        ho_Region.Dispose();
        HOperatorSet.Threshold(ho_Image, out ho_Region, 45, 255);
        ho_RegionFillUp.Dispose();
        HOperatorSet.FillUp(ho_Region, out ho_RegionFillUp);
        ho_ImageReduced.Dispose();
        HOperatorSet.ReduceDomain(ho_Image, ho_RegionFillUp, out ho_ImageReduced);
        //mean_image (ImageReduced, ImageMean, 60, 60)
        //dyn_threshold (ImageReduced, ImageMean, RegionDynThresh, 4, 'dark')
        ho_Region1.Dispose();
        HOperatorSet.Threshold(ho_ImageReduced, out ho_Region1, 0, nThreshLow);
        ho_ConnectedRegions.Dispose();
        HOperatorSet.Connection(ho_Region1, out ho_ConnectedRegions);
        ho_SelectedRegions.Dispose();
        HOperatorSet.SelectShape(ho_ConnectedRegions, out ho_SelectedRegions, "area", "and", nAreaMin, 100000); // 800 
        HOperatorSet.CountObj(ho_SelectedRegions, out hv_Number);
        nDefectNum = hv_Number;
        // Please note: In the exported code the zooming of images will not be adjusted
        // automatically (like in HDevelop). Therefore you have to call 'HOperatorSet.SetPart()'
        // with the parameters of the image you want to display.
        HOperatorSet.DispObj(ho_Image, hv_ExpDefaultWinHandle);
        // Please note: In the exported code the zooming of images will not be adjusted
        // automatically (like in HDevelop). Therefore you have to call 'HOperatorSet.SetPart()'
        // with the parameters of the image you want to display.
        HOperatorSet.DispObj(ho_SelectedRegions, hv_ExpDefaultWinHandle);
        // HOperatorSet.DumpWindow(hv_ExpDefaultWinHandle, "jpeg", "C:\\Users\\rocki\\Desktop\\captured\\dump.jpg");
        // HOperatorSet.DumpWindowImage(out imgDone, hv_ExpDefaultWinHandle);
    // HOperatorSet.dump_window (WindowID, 'jpeg', 'result')
        ho_Image.Dispose();
        ho_Region.Dispose();
        ho_RegionFillUp.Dispose();
        ho_ImageReduced.Dispose();
        ho_ConnectedRegions.Dispose();
        ho_SelectedRegions.Dispose();
        ho_Region1.Dispose();
    }

    public void InitHalcon()
    {
        // Default settings used in HDevelop 
        HOperatorSet.SetSystem("do_low_error", "false");
    }

    public void RunHalcon(HTuple Window, String strFileName, Int32 nAreaMin, Int32 nThreshLow, out Int32 nDefectNum)
    {
        hv_ExpDefaultWinHandle = Window;
        action(strFileName,  nAreaMin, nThreshLow, out nDefectNum);
        // GenertateRGBBitmap(imgProc, out imgDone);
    }

    private void GenertateRGBBitmap(HObject image, out Bitmap res)
    {
        HTuple hred, hgreen, hblue, type, width, height;
        HOperatorSet.GetImagePointer3(image, out hred, out hgreen, out hblue, out type, out width, out height);
        res = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

        Rectangle rect = new Rectangle(0, 0, width, height);
        BitmapData bitmapData = res.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

        unsafe
        {
            Byte* bptr = (Byte*)bitmapData.Scan0;
            Byte* r = ((Byte*)hred.I);
            Byte* g = ((Byte*)hgreen.I);
            Byte* b = ((Byte*)hblue.I);
            Int32 nIndex = 0;
            for (Int32 i = 0; i < width * height; i++)
            {
                bptr[nIndex] = b[i];
                bptr[nIndex + 1] = g[i];
                bptr[nIndex + 2] = r[i];
                // bptr[nIndex + 3] = 255;
                nIndex += 4;
            }
        }

        res.UnlockBits(bitmapData);
    }
}

