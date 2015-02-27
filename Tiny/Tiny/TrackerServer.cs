﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Microsoft.Kinect;
using KinectSerializer;
using System.Diagnostics;
using System.Windows.Threading;
using Tiny.UI;

namespace Tiny
{
    public class TServer
    {
        private readonly TcpListener serverKinectTCPListener;
        private readonly Thread serverThread;

        private static const uint SEC_IN_MILLISEC = 1000;
        private static const uint WRITE_LOG_INTERVAL = 1/4 * SEC_IN_MILLISEC;
        private static const uint FLUSH_LOG_INTERVAL = 3 * SEC_IN_MILLISEC;
        private static const uint FRAME_IN_SEC = 60;
        //private static const uint TRACKING_INTERVAL;
        private readonly Stopwatch writeLogStopwatch;
        private readonly Stopwatch flushLogStopwatch;
        private readonly Stopwatch trackerStopwatch;

        private readonly Tracker tracker;
        private MultipleKinectUI multipleKinectUI;
        private TrackingUI trackingUI;

        private event KinectCameraHandler OnAddedKinectCamera;
        private event KinectCameraHandler OnRemovedKinectCamera;
        private delegate void KinectCameraHandler(IPEndPoint kinectClientIP);
        private event KinectFrameHandler MultipleKinectUIUpdate;
        private delegate void KinectFrameHandler(Tracker.Result result);
        private event WorldViewHandler TrackingUIUpdate;
        private delegate void WorldViewHandler(Tracker.Result result);

        public TServer(int port, int kinectCount)
        {
            this.serverKinectTCPListener = new TcpListener(IPAddress.Any, port);
            this.serverThread = new Thread(new ThreadStart(this.ServerWorkerThread));
            
            this.tracker = new Tracker(kinectCount);

            Thread multipleKinectUIThread = new Thread(new ThreadStart(this.StartMultipleKinectUIThread));
            multipleKinectUIThread.SetApartmentState(ApartmentState.STA);
            multipleKinectUIThread.Start();

            Thread trackingUIThread = new Thread(new ThreadStart(this.StartTrackingUIThread));
            trackingUIThread.SetApartmentState(ApartmentState.STA);
            trackingUIThread.Start();

            this.writeLogStopwatch = new Stopwatch();
            this.flushLogStopwatch = new Stopwatch();
            this.trackerStopwatch = new Stopwatch();
        }

        // Run the tracking server
        public void Run()
        {
            this.serverKinectTCPListener.Start();
            this.serverThread.Start();
            this.writeLogStopwatch.Start();
            Debug.WriteLine(Tiny.Properties.Resources.SERVER_START + this.serverKinectTCPListener.LocalEndpoint);
        }

        public void Stop()
        {
            this.serverThread.Abort();
            this.writeLogStopwatch.Stop();
            this.flushLogStopwatch.Stop();
            this.trackerStopwatch.Stop();
            TLogger.Flush();
            TLogger.Close();
        }

        private void StartMultipleKinectUIThread()
        {
            this.multipleKinectUI = new MultipleKinectUI();
            this.multipleKinectUI.Show();
            this.MultipleKinectUIUpdate += this.multipleKinectUI.UpdateDisplay;
            Dispatcher.Run();
        }

        private void StartTrackingUIThread()
        {
            this.trackingUI = new TrackingUI();
            this.trackingUI.Show();
            this.TrackingUIUpdate += this.trackingUI.UpdateDisplay;
            this.OnAddedKinectCamera += this.trackingUI.AddKinectCamera;
            this.OnRemovedKinectCamera += this.trackingUI.RemoveKinectCamera;
            Dispatcher.Run();
        }

        // Accepts connections and for each thread spaw a new connection
        private void ServerWorkerThread()
        {
            while (true)
            {
                TcpClient kinectClient = this.serverKinectTCPListener.AcceptTcpClient();
                Thread kinectFrameThread = new Thread(() => this.ServerKinectFrameWorkerThread(kinectClient));
                kinectFrameThread.Start();
            }
        }

        private void ServerKinectFrameWorkerThread(object obj)
        {
            TcpClient client = obj as TcpClient;
            IPEndPoint clientIP = (IPEndPoint)client.Client.RemoteEndPoint;
            NetworkStream clientStream = client.GetStream();
            Debug.WriteLine(Tiny.Properties.Resources.CONNECTION_START + clientIP);
            
            bool kinectCameraAdded = false;

            while (true)
            {
                if (!kinectCameraAdded && this.OnAddedKinectCamera != null)
                {
                    Thread fireOnAddKinectCamera = new Thread(() => this.OnAddedKinectCamera(clientIP));
                    fireOnAddKinectCamera.Start();
                    kinectCameraAdded = true;
                }
                try
                {
                    if (!client.Connected) break;

                    while (!clientStream.DataAvailable) ;

                    SBodyFrame bodyFrame = BodyFrameSerializer.Deserialize(clientStream);
                    Thread trackingUpdateThread = new Thread(() => this.TrackingUpdateThread(clientIP, bodyFrame));
                    trackingUpdateThread.Start();

                    // Response content is trivial
                    byte[] response = Encoding.ASCII.GetBytes(Properties.Resources.SERVER_RESPONSE_OKAY);
                    clientStream.Write(response, 0, response.Length);
                    clientStream.Flush();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(Tiny.Properties.Resources.SERVER_EXCEPTION);
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                    clientStream.Close();
                    client.Close();
                }
            }
            this.tracker.RemoveClient(clientIP);
            Thread fireOnRemoveKinectCamera = new Thread(() => this.OnRemovedKinectCamera(clientIP));
            fireOnRemoveKinectCamera.Start();
            clientStream.Close();
            clientStream.Dispose();
            client.Close();
        }

        private void TrackingUpdateThread(IPEndPoint clientIP, SBodyFrame bodyFrame)
        {
            Tracker.Result result = this.tracker.SynchronizeTracking(clientIP, bodyFrame);
            this.MultipleKinectUIUpdate(result);
            this.TrackingUIUpdate(result);
            //if (this.writeLogStopwatch.ElapsedMilliseconds > this.writeLogInterval)
            //{
            //    Thread writeLogThread = new Thread(() => TLogger.Write(result));
            //    writeLogThread.Start();
            //    this.writeLogStopwatch.Restart();
            //}
        }
    }
}
