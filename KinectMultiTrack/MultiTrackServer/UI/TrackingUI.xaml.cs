﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectSerializer;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Net;
using KinectMultiTrack.WorldView;
using System.Globalization;
using System.Threading;
using KinectMultiTrack.Studies;

namespace KinectMultiTrack.UI
{
    public partial class TrackingUI : Window
    {
        private Dictionary<string, MenuItem> referenceKinectIPs;
        private string currentReferenceKinectIP;
        private enum ViewMode
        {
            All,
            Average,
            Skeletons,
        };
        private ViewMode currentViewMode;

        private DrawingGroup trackingUIDrawingGroup;
        private DrawingImage trackingUIViewSource;
        private DrawingGroup multipleUIDrawingGroup;
        private DrawingImage multipleUIViewSource;
        private UIElement trackingUIViewCopy;
        private UIElement multipleUIViewCopy;

        private KinectSensor kinectSensor;
        private CoordinateMapper coordinateMapper;

        public event TrackingUISetupHandler OnSetup;
        public delegate void TrackingUISetupHandler(int kinectCount, int studyId, int kinectConfiguration);
        public event TrackingUIHandler OnStartStop;
        public delegate void TrackingUIHandler(bool start);
        public event TrackingUIUpdateHandler OnDisplayResult;
        public delegate void TrackingUIUpdateHandler(TrackerResult result, int userScenario);

        private bool studyOn;
        private IEnumerable<UserTask> userTasks;
        private int currentTaskIdx;
        private bool toRecalibrate;

        private static readonly string UNINITIALIZED = "Uninitialized";
        private static readonly string INITIALIZED = "Initialized";
        private static readonly string RUNNING = "Server Running...";
        private static readonly string STOPPED = "Server Stopped";
        private static readonly string KINECT_FORMAT = "Waiting for Kinects...{0}";
        private static readonly string CALIBRATION_FORMAT = "Calibrating...\n{0} frames remaining";
        private static readonly string RE_CALIBRATION_FORMAT = "Confused!!!\n{0}";

        private const int SHOW_MULTI_UI_FRAME_INTERVAL = 4;
        private int currentFrameCount = 1;

        public TrackingUI()
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.referenceKinectIPs = new Dictionary<string, MenuItem>();
            this.currentReferenceKinectIP = "";
            this.currentViewMode = ViewMode.All;

            this.trackingUIDrawingGroup = new DrawingGroup();
            this.trackingUIViewSource = new DrawingImage(this.trackingUIDrawingGroup);
            this.multipleUIDrawingGroup = new DrawingGroup();
            this.multipleUIViewSource = new DrawingImage(this.multipleUIDrawingGroup);
            this.trackingUIViewCopy = this.TrackingUI_Viewbox.Child;
            this.multipleUIViewCopy = this.MultipleUI_Viewbox.Child;

            this.kinectSensor = KinectSensor.GetDefault();
            this.kinectSensor.Open();
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            this.studyOn = false;

            this.Closing += this.TrackingUI_Closing;

            //this.refreshMultipleUIStopwatch = new Stopwatch();
        }


        public void Server_AddKinectCamera(IPEndPoint clientIP)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                MenuItem kinectIPItem = new MenuItem();
                kinectIPItem.Header = clientIP.ToString();
                kinectIPItem.Click += KinectFOVItem_Click;
                this.KinectFOVMenu.Items.Add(kinectIPItem);
                this.referenceKinectIPs[clientIP.ToString()] = kinectIPItem;
            }));
        }

        public void Server_RemoveKinectCamera(IPEndPoint clientIP)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.KinectFOVMenu.Items.Remove(this.referenceKinectIPs[clientIP.ToString()]);
                this.referenceKinectIPs.Remove(clientIP.ToString());
                if (this.currentReferenceKinectIP.Equals(clientIP))
                {
                    this.KinectFOVBtn.Content = "Reference Kinect";
                    this.currentReferenceKinectIP = "";
                }
            }));
        }

        public void Tracker_OnWaitingKinects(int kinects)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.ShowProgressText(String.Format(TrackingUI.KINECT_FORMAT, kinects));
            }));
        }

        public void Tracker_OnCalibration(int framesRemaining)
        {
            if (framesRemaining == Tracker.MIN_CALIBRATION_FRAMES_STORED && toRecalibrate)
            {
                return;
            }
            this.toRecalibrate = false;
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.ShowProgressText(String.Format(TrackingUI.CALIBRATION_FORMAT, framesRemaining));
            }));
        }

        public void Tracker_OnReCalibration(string msg)
        {
            this.toRecalibrate = true;
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.ShowProgressText(String.Format(TrackingUI.RE_CALIBRATION_FORMAT, msg));
            }));
        }

        public void Tracker_OnResult(TrackerResult result, int scenario)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                this.DisplayBodyFrames(result, scenario);
            }));
        }

        private void TrackingUI_Loaded(object sender, RoutedEventArgs e)
        {
            this.ShowProgressText(TrackingUI.UNINITIALIZED);
        }

        private void TrackingUI_Closing(object sender, CancelEventArgs e)
        {
            this.OnStartStop(false);
        }

        private void SetupBtn_Click(object sender, RoutedEventArgs e)
        {
            SetupDialog setup = new SetupDialog();
            setup.Owner = this;
            setup.ShowDialog();
            if (setup.DialogResult.HasValue && setup.DialogResult.Value)
            {
                this.studyOn = setup.User_Study_On;
                this.userTasks = setup.User_Task;
                this.currentTaskIdx = 0;
                this.StartBtn.IsEnabled = true;
                this.OnSetup(setup.Kinect_Count, setup.User_Study_Id, setup.Kinect_Configuration);
                this.ShowProgressText(TrackingUI.INITIALIZED);
                if (this.userTasks.Equals(UserTask.TASK_FREE))
                {
                    this.MultipleUI_Viewbox.Child = this.multipleUIViewCopy;
                }
                if (this.studyOn || this.userTasks.Equals(UserTask.TASK_FREE))
                {
                    //this.refreshMultipleUIStopwatch.Start();
                }
            }
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            this.SetupBtn.IsEnabled = false;
            // TODO: Fix stopping server
            this.StartBtn.IsEnabled = false;
            //this.StopBtn.IsEnabled = true;
            this.RecalibrateBtn.IsEnabled = true;
            this.KinectFOVBtn.IsEnabled = true;
            this.ViewModeBtn.IsEnabled = true;
            this.OnStartStop(true);
            this.ShowProgressText(TrackingUI.RUNNING);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            this.SetupBtn.IsEnabled = true;
            this.StartBtn.IsEnabled = true;
            this.StopBtn.IsEnabled = false;
            this.RecalibrateBtn.IsEnabled = false;
            this.KinectFOVBtn.IsEnabled = false;
            this.ViewModeBtn.IsEnabled = false;
            this.OnStartStop(false);
            this.ShowProgressText(TrackingUI.STOPPED);
        }

        private void RecalibrateBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ShowProgressText(string text)
        {
            Grid textGrid = new Grid();
            textGrid.Width = 150;
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = Brushes.White;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textGrid.Children.Add(textBlock);
            this.TrackingUI_Viewbox.Child = textGrid;
        }

        #region UI viewing bindings Important!!!
        public ImageSource TrackingUI_Viewsource
        {
            get
            {
                return this.trackingUIViewSource;
            }
        }

        public ImageSource MultipleUI_Viewsource
        {
            get
            {
                return this.multipleUIViewSource;
            }
        }
        #endregion

        #region keyup
        private void TrackingUI_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (!this.studyOn || this.userTasks.Equals(UserTask.TASK_FREE))
            {
                return;
            }
            if (e.Key == Key.Down)
            {
                this.ShowNextTask();
            }
        }

        private void ShowNextTask()
        {
            Grid textGrid = new Grid();
            textGrid.Width = 150;
            TextBlock textBlock = new TextBlock();
            textBlock.Text = this.userTasks.ElementAt(this.currentTaskIdx).Description;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textGrid.Children.Add(textBlock);
            this.MultipleUI_Viewbox.Child = textGrid;
            if (this.currentTaskIdx < (this.userTasks.Count() - 1))
            {
                this.currentTaskIdx++;
            }
        }
        #endregion

        public int GetCurrentScenarioId()
        {
            return this.userTasks.ElementAt(this.currentTaskIdx).ScenarioId;
        }

        private void DisplayBodyFrames(TrackerResult result, int scenarioId)
        {
            if (result.Equals(TrackerResult.Empty))
            {
                return;
            }
            this.TrackingUI_Viewbox.Child = this.trackingUIViewCopy;
            this.RefreshTrackingUI(result);
            // For writing to log file
            if (this.studyOn)
            {
                if (scenarioId != Logger.NA)
                {
                    this.OnDisplayResult(result, scenarioId);
                }
            }
            else if (!this.studyOn || this.userTasks.Equals(UserTask.TASK_FREE))
            {
                if (this.currentFrameCount == TrackingUI.SHOW_MULTI_UI_FRAME_INTERVAL)
                {
                    this.MultipleUI_Viewbox.Child = this.multipleUIViewCopy;
                    this.RefreshMultipleUI(result);
                    this.currentFrameCount = 1;
                }
                else
                {
                    this.currentFrameCount++;
                }
            }
        }

        private TrackerResult.KinectFOV UpdateReferenceKinectFOV(IEnumerable<TrackerResult.KinectFOV> fovs)
        {
            TrackerResult.KinectFOV referenceFOV = fovs.First();
            foreach (TrackerResult.KinectFOV fov in fovs)
            {
                if (fov.ClientIP.ToString().Equals(this.currentReferenceKinectIP))
                {
                    referenceFOV = fov;
                    break;
                }
            }
            this.currentReferenceKinectIP = referenceFOV.ClientIP.ToString();
            return referenceFOV;
        }

        private void RefreshTrackingUI(TrackerResult result)
        {
            double trackingUIWidth = this.TrackingUI_Viewbox.ActualWidth;
            double trackingUIHeight = this.TrackingUI_Viewbox.ActualHeight;

            int frameWidth = result.DepthFrameWidth;
            int frameHeight = result.DepthFrameHeight;

            TrackerResult.KinectFOV referenceFOV = this.UpdateReferenceKinectFOV(result.FOVs);

            using (DrawingContext dc = this.trackingUIDrawingGroup.Open())
            {
                this.DrawBackground(Colors.BACKGROUND_TRACKING, trackingUIWidth, trackingUIHeight, dc);

                int personIdx = 0;
                foreach (TrackerResult.Person person in result.People)
                {
                    TrackingSkeleton refSkeleton = person.GetSkeletonInFOV(referenceFOV);
                    if (refSkeleton == null)
                    {
                        continue;
                    }

                    List<KinectBody> bodies = new List<KinectBody>();
                    foreach (TrackerResult.PotentialSkeleton pSkeleton in person.PotentialSkeletons)
                    {
                        if (pSkeleton.Skeleton.CurrentPosition != null)
                        {
                            this.DrawClippedEdges(pSkeleton.Skeleton.CurrentPosition.Kinect, frameWidth, frameHeight, dc);
                            bodies.Add(WBody.TransformWorldToKinectBody(pSkeleton.Skeleton.CurrentPosition.Worldview, refSkeleton.InitialAngle, refSkeleton.InitialCenterPosition));
                        }
                    }

                    Pen skeletonColor = Colors.SKELETON[personIdx++];
                    if (this.currentViewMode == ViewMode.Skeletons || this.currentViewMode == ViewMode.All)
                    {
                        this.DrawSkeletons(bodies, dc, skeletonColor);
                    }
                    if (this.currentViewMode == ViewMode.Average || this.currentViewMode == ViewMode.All)
                    {
                        this.DrawSkeletons(new List<KinectBody>() { KinectBody.GetAverageBody(bodies) }, dc, Colors.AVG_BONE);
                    }
                }
                this.DrawClipRegion(frameWidth, frameHeight, this.trackingUIDrawingGroup);
            }
        }

        private void RefreshMultipleUI(TrackerResult result)
        {
            double multipleUIWidth = this.MultipleUI_Viewbox.ActualWidth;
            double multipleUIHeight = this.MultipleUI_Viewbox.ActualHeight;

            int frameWidth = result.DepthFrameWidth;
            int frameHeight = result.DepthFrameHeight;

            using (DrawingContext dc = this.multipleUIDrawingGroup.Open())
            {
                this.DrawBackground(Colors.BACKGROUND_MULTIPLE, multipleUIWidth, multipleUIHeight, dc);

                int personIdx = 0;
                foreach (TrackerResult.Person person in result.People)
                {
                    Pen skeletonColor = Colors.SKELETON[personIdx++];
                    foreach (TrackerResult.PotentialSkeleton pSkeleton in person.PotentialSkeletons)
                    {
                        if (pSkeleton.Skeleton.CurrentPosition != null)
                        {
                            SBody body = pSkeleton.Skeleton.CurrentPosition.Kinect;
                            Dictionary<JointType, DrawableJoint> jointPts = new Dictionary<JointType, DrawableJoint>();
                            foreach (JointType jt in body.Joints.Keys)
                            {
                                Point point = new Point(body.Joints[jt].DepthSpacePoint.X, body.Joints[jt].DepthSpacePoint.Y);
                                jointPts[jt] = new DrawableJoint(point, body.Joints[jt].TrackingState);
                            }
                            this.DrawBody(jointPts, dc, skeletonColor);
                        }
                    }
                }
                this.DrawClipRegion(frameWidth, frameHeight, this.multipleUIDrawingGroup);
            }
        }

        private void DrawBackground(Brush color, double width, double height, DrawingContext dc)
        {
            dc.DrawRectangle(color, null, new Rect(0.0, 0.0, width, height));
        }

        private void DrawClipRegion(int frameWidth, int frameHeight, DrawingGroup dg)
        {
            dg.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, frameWidth, frameHeight));
        }

        private void DrawClippedEdges(SBody body, int frameWidth, int frameHeight, DrawingContext dc)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                dc.DrawRectangle(Colors.CLIPPED_EDGES, null, new Rect(0, frameHeight - Colors.CLIP_BOUNDS_THICKNESS, frameWidth, Colors.CLIP_BOUNDS_THICKNESS));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                dc.DrawRectangle(Colors.CLIPPED_EDGES, null, new Rect(0, 0, frameWidth, Colors.CLIP_BOUNDS_THICKNESS));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                dc.DrawRectangle(Colors.CLIPPED_EDGES, null, new Rect(0, 0, Colors.CLIP_BOUNDS_THICKNESS, frameHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                dc.DrawRectangle(Colors.CLIPPED_EDGES, null, new Rect(frameWidth - Colors.CLIP_BOUNDS_THICKNESS, 0, Colors.CLIP_BOUNDS_THICKNESS, frameHeight));
            }
        }

        private void DrawSkeletons(IEnumerable<KinectBody> bodies, DrawingContext dc, Pen skeletonColor)
        {
            foreach (KinectBody body in bodies)
            {
                Dictionary<JointType, DrawableJoint> jointPts = new Dictionary<JointType, DrawableJoint>();
                foreach (JointType jt in body.Joints.Keys)
                {
                    CameraSpacePoint position = body.Joints[jt].Position;
                    if (position.Z < 0)
                    {
                        position.Z = 0.1f;
                    }
                    DepthSpacePoint jointPt2D = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                    jointPts[jt] = new DrawableJoint(new Point(jointPt2D.X, jointPt2D.Y), body.Joints[jt].TrackingState);
                }
                this.DrawBody(jointPts, dc, skeletonColor);
            }
        }

        private void DrawBody(Dictionary<JointType, DrawableJoint> joints, DrawingContext dc, Pen skeletonColor)
        {
            // Draw bones
            foreach (var bone in SkeletonStructure.Bones)
            {
                JointType jt1st = bone.Item1;
                JointType jt2nd = bone.Item2;
                if (!joints.ContainsKey(jt1st) || !joints.ContainsKey(jt2nd))
                {
                    continue;
                }
                if (joints[jt1st].TrackingState == TrackingState.NotTracked || joints[jt2nd].TrackingState == TrackingState.NotTracked)
                {
                    continue;
                }
                else if (joints[jt1st].TrackingState == TrackingState.Tracked && joints[jt2nd].TrackingState == TrackingState.Tracked)
                {
                    this.DrawBone(joints[jt1st].Point, joints[jt2nd].Point, dc, skeletonColor);
                }
                else
                {
                    this.DrawBone(joints[jt1st].Point, joints[jt2nd].Point, dc, Colors.INFERRED_BONE);
                }
            }
            // Draw joints
            foreach (DrawableJoint joint in joints.Values)
            {
                if (joint.TrackingState == TrackingState.NotTracked)
                {
                    continue;
                }
                else if (joint.TrackingState == TrackingState.Tracked)
                {
                    this.DrawJoint(joint.Point, dc, Colors.TRACKED_JOINT, Colors.JOINT_THICKNESS);
                }
                else if (joint.TrackingState == TrackingState.Inferred)
                {
                    this.DrawJoint(joint.Point, dc, Colors.INFERRED_JOINT, Colors.JOINT_THICKNESS);
                }
            }
        }

        private void DrawJoint(Point joint, DrawingContext dc, Brush brush, double thickness)
        {
            dc.DrawEllipse(brush, null, joint, thickness, thickness);
        }

        private void DrawBone(Point from, Point to, DrawingContext dc, Pen pen)
        {
            dc.DrawLine(pen, from, to);
        }

        private void KinectFOVBtn_Click(object sender, RoutedEventArgs e)
        {
            Button referenceKinectBtn = sender as Button;
            referenceKinectBtn.ContextMenu.IsEnabled = true;
            referenceKinectBtn.ContextMenu.PlacementTarget = referenceKinectBtn;
            referenceKinectBtn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            referenceKinectBtn.ContextMenu.IsOpen = true;
        }


        private void KinectFOVItem_Click(object sender, RoutedEventArgs e)
        {
            string referenceKinectIP = (sender as MenuItem).Header.ToString();
            this.currentReferenceKinectIP = referenceKinectIP;
            this.KinectFOVBtn.Content = referenceKinectIP;
        }

        private void ViewModeBtn_Click(object sender, RoutedEventArgs e)
        {
            Button viewModeBtn = sender as Button;
            viewModeBtn.ContextMenu.IsEnabled = true;
            viewModeBtn.ContextMenu.PlacementTarget = viewModeBtn;
            viewModeBtn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            viewModeBtn.ContextMenu.IsOpen = true;
        }

        private void ViewMode_Skeletons_Click(object sender, RoutedEventArgs e)
        {
            this.currentViewMode = ViewMode.Skeletons;
            this.ViewModeBtn.Content = ViewMode.Skeletons;
        }

        private void ViewMode_Average_Click(object sender, RoutedEventArgs e)
        {
            this.currentViewMode = ViewMode.Average;
            this.ViewModeBtn.Content = ViewMode.Average;
        }

        private void ViewMode_All_Click(object sender, RoutedEventArgs e)
        {
            this.currentViewMode = ViewMode.All;
            this.ViewModeBtn.Content = ViewMode.All;
        }
    }
}

