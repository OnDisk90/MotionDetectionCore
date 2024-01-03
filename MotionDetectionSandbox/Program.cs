using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace MotionDetectionSandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initiate Camera 0, please wait ...");
            bool recFlag = true;

            VideoCapture capture = new VideoCapture(0);

            var afWindow = new Window("Annotated Frame");
            var cdWindow = new Window("Contour Delta");
            
         //   Console.WriteLine("Open CV Build Info " + Cv2.GetBuildInformation());


            //VideoCapture capture = new VideoCapture("http://pendelcam.kip.uni-heidelberg.de/mjpg/video.mjpg");
          
            //VideoCapture capture = new VideoCapture("rtsp://b03773d78e34.entrypoint.cloud.wowza.com:1935/app-4065XT4Z/80c76e59_stream1");
           

            


            int frameIndex = 0;
            Mat lastFrame = new Mat();
            VideoWriter writer = null;
            Console.WriteLine("Start Capture");

            while (capture.IsOpened())
            {
                Mat frame = new Mat();
                int recordFrames = 0;

                if (!capture.Read(frame))
                    break;

                Mat grayFrame, dilatedFrame, edges, deltaCopyFrame = new Mat();
                Mat deltaFrame = new Mat();

                try
                {
                    frame = frame.Resize(new Size(0, 0), 0.6, 0.6);
                }
                catch (Exception e)
                {

                }
                grayFrame = frame.CvtColor(ColorConversionCodes.BGR2GRAY);
                grayFrame = grayFrame.GaussianBlur(new Size(21, 21), 0);

                if ((frameIndex == 0) && (recFlag))
                {
                    frameIndex++;

                    afWindow.Move(0, 0);
                    cdWindow.Move(0, grayFrame.Size().Height);

                    string fileName = "C:\\temp\\capture.avi";

                    //string fcc = capture.FourCC;
                    var fcc = FourCC.XVID;
                    double fps = capture.Fps;
                    if (fps <= 0)
                        fps = 25;

                    Size frameSize = new Size(grayFrame.Size().Width, grayFrame.Size().Height);

                    writer = new VideoWriter(fileName, fcc, fps, frameSize);
                    Console.Out.WriteLine("Frame Size = " + grayFrame.Size().Width + " x " + grayFrame.Size().Height);

                    if (!writer.IsOpened())
                    {
                        Console.Out.WriteLine("Error Opening Video File For Write");
                        return;
                    }
                    else
                        Console.Out.WriteLine("Start Recording " + fileName);

                    lastFrame = grayFrame;
                    continue;
                }
                else if (frameIndex % 50 == 0)
                {
                    frameIndex = 0;
                    lastFrame = grayFrame;
                }

                frameIndex++;

                Cv2.Absdiff(lastFrame, grayFrame, deltaFrame);
                Cv2.Threshold(deltaFrame, deltaFrame, 50, 255, ThresholdTypes.Binary);

                int iterations = 2;
                Cv2.Dilate(deltaFrame, deltaFrame, new Mat(), new Point(), iterations);

                Point[][] contours;
                HierarchyIndex[] hierarchy;

                Cv2.FindContours(deltaFrame, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple, new Point(0, 0));

                var countorsPoly = new Point[contours.Length][];
                List<Rect> boundRect = new List<Rect>();
                List<Point2f> center = new List<Point2f>();
                List<float> radius = new List<float>();

                for (int i = 0; i < contours.Length; i++)
                {
                    countorsPoly[i] = Cv2.ApproxPolyDP(contours[i], 3, true);
                    if (countorsPoly.Length != 0)
                    {
                        boundRect.Insert(i, Cv2.BoundingRect(countorsPoly[i]));
                        Cv2.MinEnclosingCircle(countorsPoly[i], out Point2f centerObj, out float radiusObj);
                        center.Insert(i, centerObj);
                        radius.Insert(i, radiusObj);
                    }
                }

                for (int i = 0; i < contours.Length; i++)
                {
                    if (countorsPoly.Length != 0)
                    {
                        Scalar color = new Scalar(54, 67, 244);
                        //Cv2.DrawContours(frame, countorsPoly, i, color, 1, LineTypes.Link8, new HierarchyIndex[] { }, 0, new Point());
                        Cv2.Rectangle(frame, boundRect[i].TopLeft, boundRect[i].BottomRight, color, 2, LineTypes.Link8, 0);
                        //Cv2.Circle(frame, (int)center[i].X, (int)center[i].Y, (int)radius[i], color, 2, LineTypes.Link8, 0);
                        recordFrames = 90;
                    }
                }

                afWindow.ShowImage(frame);
                cdWindow.ShowImage(deltaFrame);
                if ((writer != null) && (recordFrames > 0))
                {
                    frame.PutText(DateTime.Now.ToString(), new OpenCvSharp.Point(0, 30), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 0, 0), 2, LineTypes.AntiAlias);               
                    writer.Write(frame);
                    recordFrames--;
                }

                switch(Cv2.WaitKey(1))
                {
                    case 27:
                        capture.Release();
                        if (writer != null)
                            writer.Release();
                        return;
                }
            }
        }
    }
}
