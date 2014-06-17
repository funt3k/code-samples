using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Speech;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Kinect.Toolkit.Interaction;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;



namespace KinectDemo
{

    public class KinectControl
    {
        private KinectSensor sensor;
        private SpeechRecognitionEngine speechEngine;
        private SkeletonStream ss;
        private InteractionStream inStream;
        private interactionAdapter intAdapter;
        private InteractionHandType handType;
        private int frameout;
        private double initialValue;
        private double currentValue;
        private int skeletonID;
        /* State 0 - Kinect is turned on
         * State 1 - Voice recognition is enabled, program awaits command  
         * State 2 - Command is received, Skeletonstream is enabled, program waits for grip;
         * state 3 - program tracks writstjoint position, releasing the hand will move to state 0
         */
        private int state;
        private String command;
        public enum commands { Brightness, Contrast, Zoom, None };
        public event EventHandler<VoiceCommandEventArgs> VoiceCommandEvent;
        public event EventHandler<GestureEventArgs> GestureEvent;

        public KinectControl(KinectSensor somesensor)
        {
            this.sensor = somesensor;
            this.state = 0; // start with state 0
            frameout = 0;
            // see if the Kinect is there
            if (!sensor.IsRunning)
            {
                try
                {
                    sensor.Start();
                    SkeletonInitialization();
                    AudioInitialization();
                    state = 1; // move to state 1;
                    //onRaisedVoiceCommand(new VoiceCommandEventArgs("Connected to Kinect"));
                }
                catch(IOException)
                {
                    this.sensor = null;
                    //onRaisedVoiceCommand(new VoiceCommandEventArgs("Unable to start to Kinect"));
                }
            }
        }

        
        private void AudioInitialization()
        {
            
            // voice recognition initialization
            RecognizerInfo ri = null;
            //sensor.AudioSource.AutomaticGainControlEnabled = false;
            sensor.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    ri = recognizer;
                }
            }
            this.command = null;
            // voice command library
            var options= new Choices();
            options.Add(new SemanticResultValue("zoom", "ZOOM"));
            options.Add(new SemanticResultValue("brightness", "BRIGHTNESS"));
            options.Add(new SemanticResultValue("contrast", "CONTRAST"));
            options.Add(new SemanticResultValue("Stop", "STOP"));

            this.speechEngine = new SpeechRecognitionEngine(ri);
            var gb = new GrammarBuilder { Culture = ri.Culture };
            gb.Append(options);
            var g = new Grammar(gb);
            speechEngine.LoadGrammar(g);
            //speechEngine.SpeechRecognitionRejected += speechEngine_SpeechRecognitionRejected;
            speechEngine.SpeechRecognized += speechEngine_SpeechRecognized;
            //speechEngine.SpeechHypothesized += speechEngine_SpeechHypothesized;
            var kinectstream = sensor.AudioSource.Start();
            speechEngine.SetInputToAudioStream(kinectstream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
             
        }

        void speechEngine_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (state == 1)
            {
                //onRaisedVoiceCommand(new VoiceCommandEventArgs("Voice command hypothesized to be " + e.Result.Semantics.Value + " at " + e.Result.Confidence + " confidence"));
            }
            return;
        }

        private void speechEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            const double confidenceValue = 0.5;
            if (e.Result.Confidence > confidenceValue && e.Result.Semantics.Value.ToString() == "STOP")
            {
                onRaisedVoiceCommand(new VoiceCommandEventArgs("Voice command Stop recognized at " + e.Result.Confidence + " confidence"));
                state = 1;
                return;
            }
            
            if ((e.Result.Confidence > confidenceValue) && (state == 1) )
            {
                this.command = e.Result.Semantics.Value.ToString();
                switch (e.Result.Semantics.Value.ToString())
                {
                    case "BRIGHTNESS":
                        onRaisedVoiceCommand(new VoiceCommandEventArgs("Voice command brightness recognized at "+ e.Result.Confidence + " confidence"));
                        state = 2;
                        break;
                    case "CONTRAST":
                        onRaisedVoiceCommand(new VoiceCommandEventArgs("Voice command contrast recognized at " + e.Result.Confidence + " confidence"));
                       state = 2;
                        break;
                    case "ZOOM":
                        onRaisedVoiceCommand(new VoiceCommandEventArgs("Voice command zoom recognized at " + e.Result.Confidence + " confidence"));
                        state = 2;
                        break;
                }
            }
        }

        protected virtual void onRaisedVoiceCommand(VoiceCommandEventArgs voiceCommandEventArgs)
        {
            //System.Console.WriteLine("Event is raised");
            EventHandler<VoiceCommandEventArgs> handler = VoiceCommandEvent;
            if (handler != null)
            {
                handler(this, voiceCommandEventArgs);
            }

        }

        protected virtual void onRaisedGestureCommand(GestureEventArgs gestureEventArgs)
        {
            EventHandler<GestureEventArgs> handler = GestureEvent;
            if (handler != null)
            {
                handler(this, gestureEventArgs);
            }

        }

        private void speechEngine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            //const double confidenceValue = 0.5;
            if (state == 1)
            {
                onRaisedVoiceCommand(new VoiceCommandEventArgs("Voice command Rejected"));
            }
            return;
        }
        
        private void SkeletonInitialization()
        {

            this.sensor.DepthStream.Enable();
            this.sensor.DepthFrameReady += sensor_DepthFrameReady;
            //sensor.ColorStream.Enable();
            this.sensor.SkeletonStream.Enable();
            this.ss = sensor.SkeletonStream;
            ss.EnableTrackingInNearRange = true;
            this.sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;

            this.intAdapter = new interactionAdapter();
            inStream = new InteractionStream(sensor,this.intAdapter);
            inStream.InteractionFrameReady += inStream_InteractionFrameReady;

        }

        void inStream_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            // if wrong state
            //onRaisedVoiceCommand(new VoiceCommandEventArgs("I'm in state" + state + " and I got interaciton data"));
            if (state < 2 || state > 3 )
            {
                return;
            }
            if (frameout < 4)
            {
                frameout ++;
                return;
            }
            //onRaisedVoiceCommand(new VoiceCommandEventArgs("I'm in state" + state));
            InteractionFrame intFrame = e.OpenInteractionFrame();
            UserInfo[] userInfos = new UserInfo[6];
            if (intFrame != null)
            {
                intFrame.CopyInteractionDataTo(userInfos); // copy userinfos to array
                frameout = 0;
            }
            else
            {
                return;
            }
            foreach (UserInfo userInfo in userInfos)
            {
                foreach (InteractionHandPointer handPointer in userInfo.HandPointers)
                {
                    // if at state 2 and grip is detected
                    if (handPointer.IsPrimaryForUser && handPointer.IsTracked &&
                        handPointer.HandEventType == InteractionHandEventType.Grip && state == 2)
                    {
                        skeletonID = userInfo.SkeletonTrackingId;
                        if (this.command != "ZOOM")
                        {
                            initialValue = handPointer.RawY;
                        }
                        else
                        {
                            initialValue = handPointer.RawZ;
                        }
                        handType = handPointer.HandType;
                        String message = "Gesture command " + command.ToString() + " began with an initial value of " + initialValue;
                        //onRaisedGestureCommand(new GestureEventArgs(message, command, initialValue));
                        onRaisedVoiceCommand(new VoiceCommandEventArgs(message));
                        state = 3; // move onto next stage
                    }
                    // at state 3 grip has not released
                    if (handPointer.IsPrimaryForUser && handPointer.IsTracked && userInfo.SkeletonTrackingId == skeletonID &&
                        handPointer.HandEventType == InteractionHandEventType.None && state == 3 && handPointer.HandType == handType)
                    {
                        if (this.command != "ZOOM")
                        {
                            currentValue = handPointer.RawY;
                        }
                        else
                        {
                            currentValue = handPointer.RawZ;
                        }
                        String message = "Gesture command " + command.ToString() + " with a value of " + currentValue;
                        onRaisedGestureCommand(new GestureEventArgs(message, command, initialValue - currentValue));
                    }
                    // grip released detected at state 3
                    if (handPointer.IsPrimaryForUser && handPointer.IsTracked && userInfo.SkeletonTrackingId == skeletonID &&
                        handPointer.HandEventType == InteractionHandEventType.GripRelease && state == 3 && handPointer.HandType == handType)
                    {
                        //currentValue = handPointer.RawY;
                        String message = "Gesture command " + command.ToString() + " ended with a value of " + currentValue + " and initial value of " + initialValue + " and a difference of " + (currentValue - initialValue);
                        onRaisedGestureCommand(new GestureEventArgs(message, command, initialValue - currentValue));
                        state = 1; // return to initial stage
                    }
                }
            }
        }

        private void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            //onRaisedVoiceCommand(new VoiceCommandEventArgs("I'm in state" + state + " and I got depth data"));
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    inStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);

                }
            }
        }

        private void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
            //double handposition = 0;
            //onRaisedVoiceCommand(new VoiceCommandEventArgs("I'm in state" + state +" and I got skeleton data"));
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                    inStream.ProcessSkeleton(skeletons, sensor.AccelerometerGetCurrentReading(), skeletonFrame.Timestamp);
                }
                /*
                foreach (Skeleton skeleton in skeletons)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        Joint wristjoint = skeleton.Joints[JointType.WristLeft];
                        if (wristjoint.TrackingState == JointTrackingState.Tracked)
                        {
                            if (wristjoint.Position.Y > handposition)
                            {
                                handposition = wristjoint.Position.Y;
                            }
                        }
                        wristjoint = skeleton.Joints[JointType.WristRight];
                        if (wristjoint.TrackingState == JointTrackingState.Tracked)
                        {
                            if (wristjoint.Position.Y > handposition)
                            {
                                handposition = wristjoint.Position.Y;
                            }
                        }
                    }
                }
                */
            }
        }
        private void gotostateone()
        {
            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            sensor.DepthStream.Disable();
            sensor.SkeletonStream.Disable();
        }
        private void gotostatetwo()
        {
            speechEngine.RecognizeAsyncStop();
            sensor.DepthStream.Enable();
            sensor.SkeletonStream.Enable();
        }
    }

    public class interactionAdapter : IInteractionClient
    {

        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
        {
            InteractionInfo intInfo = new InteractionInfo();
            intInfo.IsGripTarget = true;
            intInfo.IsPressTarget = false;
            return intInfo;
        }
    }

    public class VoiceCommandEventArgs : EventArgs
    {
        private String debugoutput;

        public VoiceCommandEventArgs(String message)
        {
            debugoutput = message;
        }

        public String debugOutput
        {
            get { return debugoutput; }
            set { debugoutput = value; }
        }
    }

    public class GestureEventArgs : EventArgs
    {
        private String debugoutput;
        //private Boolean brightness;
        //private Boolean contrast;
        //private Boolean zoom;
        private double propertyvalue;
        private String commandtype;

        public GestureEventArgs(String message, string Type, double propertyValue)
        {
            debugoutput = message;
            propertyvalue = propertyValue;
            commandtype = Type;
            /*
            switch (commandType)
            {
                case "brightness":
                    brightness = true;
                    contrast = false;
                    zoom = false;
                    break;
                case "contrast":
                    brightness = false;
                    contrast = true;
                    zoom = false;
                    break;
                case "zoom":
                    brightness = false;
                    contrast = false;
                    zoom = true;
                    break;
            }
             */
        }
        public String debugOutput
        {
            get { return debugoutput; }
            set { debugoutput = value; }
        }
        public double propertyValue
        {
            get { return propertyvalue; }
            set { propertyvalue = value; }
        }
        public String commandType
        {
            get { return commandtype; }
            set { commandtype = value; }
        }
    }
}
