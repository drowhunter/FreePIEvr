
using com.rotovr.sdk.Telemetry;
using com.rotovr.sdk.Utility;

using System;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#if !NO_UNITY
using UnityEngine;
#else

#endif


namespace com.rotovr.sdk
{
    /// <summary>
    /// The Roto class provides an API to interact with the RotoVR chair, allowing for communication with the device, 
    /// mode setting, rotation control, calibration, and connection management.
    /// </summary>
    public class Roto
    {
        static Roto s_Roto;

        /// <summary>
        /// Gets the singleton instance of the Roto manager.
        /// Ensures only one instance of Roto exists throughout the application.
        /// </summary>
        /// <returns>The singleton instance of the Roto manager.</returns>
        public static Roto GetManager()
        {
            if (s_Roto == null)
            {
                s_Roto = new Roto();
            }

            return s_Roto;
        }

        

        RotoDataModel m_RotoData = new();
        DeviceDataModel m_ConnectedDevice;

#if !NO_UNITY
        readonly string m_CalibrationKey = "CalibrationKey";
        Transform m_ObservableTarget;
        Coroutine m_TargetRoutine;
#else
        Func<float?> m_ObservableTarget;
        CancellationTokenSource m_CancelSource;
        Stopwatch _angleUpdateStopwatch = new Stopwatch();
#endif
        bool m_IsInit;
        float? m_StartTargetAngle = null;
        int m_StartRotoAngle;


        EnforcedQueue<(long elapsedMs, float angle)> m_Queue = new (3);

        EnforcedQueue<(double x, double y)> m_directions = new(1);

        ILerper m_yawInterpolator = new Lerper();

        public float CalculateAngularVelocity()
        {
                
            
            (long elapsedTime, float angle)[] x;
                
            x = m_Queue.ToArraySafe();
                
            var avg = new List<float>();

            for (var i = 0; i < x.Length; i++)
            {
                if (i == 0 || x[i].elapsedTime == 0)
                    continue;

                var dA = Math.Abs(x[i].angle - x[i - 1].angle);
                if (dA > 180)
                    dA = 360 - dA;

                var dT = x[i].elapsedTime;  //(x[i].time - x[i - 1].time).TotalMilliseconds;
                
                var v = (dA / dT) * 1000f;

                //if velocity is impossible ignore it
                if (v < 120) 
                    avg.Add(v);

            }

            if (avg.Any())
            {
                var retval = avg.Average();
                return retval;
            }

            return 0;
        }

        ConnectionType m_ConnectionType;


        /// <summary>
        /// The current connection status of the RotoVR chair.
        /// Possible statuses: Disconnected, Connecting, or Connected.
        /// </summary>
        public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Disconnected;

        /// <summary>
        /// Event triggered when the system mode changes.
        /// </summary>
        public event Action<ModeType> OnRotoMode;

        /// <summary>
        /// Event triggered when chair data changes.
        /// </summary>
        public event Action<RotoDataModel> OnDataChanged;

        /// <summary>
        /// Event triggered when the system connection status changes.
        /// </summary>
        public event Action<ConnectionStatus> OnConnectionStatus;

        void Call(string command, string data)
        {
#if !NO_UNITY
            BleManager.Instance.Call(command, data);
#endif
        }

        /// <summary>
        /// Invoke to send BleMessage to java library
        /// </summary>
        /// <param name="message">Ble message</param>
        void SendMessage(BleMessage message)
        {
            Call(message.MessageType.ToString(), message.Data);
        }

        /// <summary>
        /// Subscribe to ble json message
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="action">Handler</param>
        public void Subscribe(string command, Action<string> action)
        {
#if !NO_UNITY
            BleManager.Instance.Subscribe(command, action);
#endif
        }

        /// <summary>
        /// Subscribe from ble json message
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="action">Handler</param>
        public void UnSubscribe(string command, Action<string> action)
        {
#if !NO_UNITY
            BleManager.Instance.UnSubscribe(command, action);
#endif
        }
        

        /// <summary>
        /// Initializes the Roto manager with a specified connection type.
        /// </summary>
        /// <param name="connectionType">The connection type (e.g., Simulation or Chair) used for communication.</param>
        public void Initialize(ConnectionType connectionType)
        {
            if (m_IsInit)
                return;

            m_IsInit = true;
            m_ConnectionType = connectionType;
            
#if !UNITY_EDITOR && !NO_UNITY
            BleManager.Instance.Init();
            Subscribe(MessageType.ModelChanged.ToString(), OnModelChangeHandler);
            Subscribe(MessageType.DeviceConnected.ToString(),
                (data) => { OnConnectionStatus?.Invoke(ConnectionStatus.Connected); });
            Subscribe(MessageType.Disconnected.ToString(),
                (data) => { OnConnectionStatus?.Invoke(ConnectionStatus.Disconnected); });
#endif
        }

        void OnConnectionStatusChange(ConnectionStatus status)
        {
            ConnectionStatus = status;
            OnConnectionStatus?.Invoke(status);
        }

        void OnModelChangeHandler(string data)
        {

            var model = new RotoDataModel(data);

            if (model.Mode != m_RotoData.Mode)
            {
                if (Enum.TryParse(model.Mode, out ModeType value))
                {
                    OnRotoMode?.Invoke(value);
                }
            }

            OnDataChanged?.Invoke(model);
            m_RotoData = model;
        }
       
        void OnModelChangeHandler(RotoDataModel model)
        {
            
            
            
            m_yawInterpolator.UpdateValue(model.Angle);

            //telemetry.ActualAngle = m_RotoData.Angle;
            //telemetry.AngularVelocity = m_AngularVelocity;

            //tel.Send(telemetry);
            //oXRMC.yaw = -model.Angle;

            //mComp.Send(oXRMC);

            if (model.Mode != m_RotoData.Mode)
            {
                if (Enum.TryParse(model.Mode, out ModeType value))
                {
                    OnRotoMode?.Invoke(value);
                }
            }

            OnDataChanged?.Invoke(model);
            m_RotoData = model;
        }

        void Scan()
        {
            SendMessage(new ScanMessage());
        }

        /// <summary>
        /// Connects to a specified device by name.
        /// </summary>
        /// <param name="deviceName">The name of the device to connect to.</param>
        internal void Connect(string deviceName)
        {
#if !UNITY_EDITOR && !NO_UNITY
            if (m_ConnectedDevice == null)
            {
                void Connected(string data)
                {
                    s_Roto.UnSubscribe(MessageType.Connected.ToString(), Connected);
                    m_ConnectedDevice = new DeviceDataModel(data);
                }

                s_Roto.Subscribe(MessageType.Connected.ToString(), Connected);

                SendMessage(
                    new ConnectMessage(new DeviceDataModel(deviceName, string.Empty).ToJson()));
            }
            else
            {
                SendMessage(new ConnectMessage(m_ConnectedDevice.ToJson()));
            }
            
#else
            if (m_ConnectionType == ConnectionType.Chair)
            {
                UsbConnector.Instance.OnConnectionStatus += OnConnectionStatusChange;
                UsbConnector.Instance.OnDataChange += OnModelChangeHandler;
                UsbConnector.Instance.Connect();
            }
            else
            {
                OnConnectionStatusChange(ConnectionStatus.Connected);
            }
#endif
        }

        /// <summary>
        /// Disconnects from the currently connected device.
        /// </summary>
        /// <param name="deviceName">The name of the device to disconnect from.</param>
        internal void Disconnect(string deviceName)
        {
#if !UNITY_EDITOR && !NO_UNITY
            if (m_ConnectedDevice != null && m_ConnectedDevice.Name == deviceName)
            {
                SendMessage(new DisconnectMessage(m_ConnectedDevice.ToJson()));
            }

#else
            if (m_ObservableTarget != null)
                m_ObservableTarget = null;
            
            GC.Collect();
            if (m_ConnectionType == ConnectionType.Chair)
            {
                UsbConnector.Instance.OnConnectionStatus -= OnConnectionStatusChange;
                UsbConnector.Instance.OnDataChange -= OnModelChangeHandler;
                UsbConnector.Instance.Disconnect();
            }
            else
            {
                OnConnectionStatusChange(ConnectionStatus.Disconnected);
            }

#endif
        }

        int _followPower;
        /// <summary>
        /// Sets the mode for the RotoVR chair with specific mode parameters.
        /// </summary>
        /// <param name="mode">The mode to set for the chair (e.g., HeadTrack, FreeMode).</param>
        /// <param name="modeParams">The mode parameters (e.g., power, deltaTargetAngle limits) to configure the chair in the specified moFollowde.</param>
        public void SetMode(ModeType mode, ModeParams modeParams)
        {
            var parametersModel = new ModeParametersModel(modeParams);

            _followPower = (mode == ModeType.FollowObject) ? modeParams.MaxPower : 100;

#if !UNITY_EDITOR && !NO_UNITY
            SendMessage(
                new SetModeMessage(new ModeModel(mode.ToString(), parametersModel).ToJson())
                );
#else
            if (m_ConnectionType == ConnectionType.Chair)
            {
                if(mode == ModeType.JoystickMode)
                    UsbConnector.Instance.SetMode(new ModeModel(ModeType.FollowObject.ToString(), parametersModel));
                else
                    UsbConnector.Instance.SetMode(new ModeModel(mode.ToString(), parametersModel));
            }
#endif
        }

        /// <summary>
        /// Sets the power of the RotoVR chair when in FreeMode. The power value must be between 30 and 100.
        /// </summary>
        /// <param name="power">The rotation power to set for the chair (valid range is 30-100).</param>
        public void SetPower(int power)
        {
            var modeParams = new ModeParams
            {
                CockpitAngleLimit = m_RotoData.TargetCockpit,
                MaxPower = power
            };

            SetMode(m_RotoData.ModeType, modeParams);
        }

        /// <summary>
        /// Calibrates the RotoVR chair, resetting the deltaTargetAngle based on the specified calibration mode.
        /// </summary>
        /// <param name="calibrationMode">The calibration mode (e.g., set to zero, set to last position).</param>
        public void Calibration(CalibrationMode calibrationMode)
        {
            switch (calibrationMode)
            {
#if !NO_UNITY
                case CalibrationMode.SetCurrent:
                    PlayerPrefs.SetInt(m_CalibrationKey, m_RotoData.Angle);
                    break;
                case CalibrationMode.SetLast:
                    if (PlayerPrefs.HasKey(m_CalibrationKey))
                    {
                        var defaultAngle = PlayerPrefs.GetInt(m_CalibrationKey);
                        RotateToAngle(GetDirection(defaultAngle, m_RotoData.Angle), defaultAngle, 100);
                    }
                    else
                        RotateToAngle(GetDirection(0, m_RotoData.Angle), 0, 100);

                    break;
#endif
                case CalibrationMode.SetToZero:
                    RotateToAngle(GetDirection(0, m_RotoData.Angle), 0, 30);
                    break;
            }
        }

        /// <summary>
        /// Rotates the RotoVR chair to the specified deltaTargetAngle.
        /// This is applicable only in the Calibration or CockpitMode.
        /// </summary>
        /// <param name="direction">The direction in which to rotate (e.g., left or right).</param>
        /// <param name="angle">The deltaTargetAngle to rotate to.</param>
        /// <param name="power">The power of rotation (valid range is 0-100).</param>
        public void RotateToAngle(Direction direction, int angle, int power)
        {
            if (angle == m_RotoData.Angle)
                return;
#if !UNITY_EDITOR && !NO_UNITY
            SendMessage(new RotateToAngleMessage(
                new RotateToAngleModel(angle, power, direction.ToString()).ToJson()));
#else
            if (m_ConnectionType == ConnectionType.Chair)
            {
                UsbConnector.Instance.TurnToAngle(new RotateToAngleModel(angle, power, direction.ToString()));
            }
            else
            {
                m_RotoData = new RotoDataModel()
                {
                    Angle = angle
                };
                OnDataChanged?.Invoke(m_RotoData);
            }
#endif
        }

        /// <summary>
        /// Rotates the chair to the closest deltaTargetAngle, choosing the best direction automatically.
        /// </summary>
        /// <param name="angle">The target deltaTargetAngle to rotate to.</param>
        /// <param name="power">The power of rotation (valid range is 0-100).</param>
        public void RotateToClosestAngleDirection(int angle, int power)
        {
            if (angle == m_RotoData.Angle)
                return;
#if !UNITY_EDITOR && !NO_UNITY
            var rotateToAngleModel = new RotateToAngleModel(angle, power,
                GetDirection(angle, m_RotoData.Angle).ToString());
            SendMessage(new RotateToAngleMessage(rotateToAngleModel.ToJson()));
#else
            if (m_ConnectionType == ConnectionType.Chair)
            {
                UsbConnector.Instance.TurnToAngle(new RotateToAngleModel(angle, power,
                    GetDirection(angle, m_RotoData.Angle).ToString()));
            }
            else
            {
                m_RotoData = new RotoDataModel()
                {
                    Angle = angle
                };
                OnDataChanged?.Invoke(m_RotoData);
            }
#endif
        }

        /// <summary>
        /// Will rotate chair on specific deltaTargetAngle with specified direction.
        /// </summary>
        /// <param name="angle">Rotation deltaTargetAngle.</param>
        /// <param name="direction">Rotation direction.</param>
        /// <param name="power">Rotational power. Can range from 0 to 100.</param>
        public void Rotate(Direction direction, int angle, int power)
        {
            var targetAngle = 0;
            switch (direction)
            {
                case Direction.Left:
                    targetAngle = m_RotoData.Angle - angle;
                    break;
                case Direction.Right:
                    targetAngle = m_RotoData.Angle + angle;
                    break;
            }
#if !UNITY_EDITOR && !NO_UNITY
            var rotateToAngleModel = new RotateToAngleModel(NormalizeAngle(targetAngle), power, direction.ToString());
            SendMessage(new RotateToAngleMessage(rotateToAngleModel.ToJson()));
#else
            if (m_ConnectionType == ConnectionType.Chair)
            {
                UsbConnector.Instance.TurnToAngle(new RotateToAngleModel(NormalizeAngle(targetAngle), power,
                    direction.ToString()));
            }
            else
            {
                m_RotoData = new RotoDataModel()
                {
                    Angle = NormalizeAngle(targetAngle)
                };
                OnDataChanged?.Invoke(m_RotoData);
            }
#endif
        }


#if !NO_UNITY
        /// <summary>
        /// Follow rotation of a target object
        /// </summary>
        /// <param name="behaviour">Target that will be used as the rotation preference.</param>
        /// <param name="target">Target object which rotation need to follow</param>
        public void FollowTarget(MonoBehaviour behaviour, Transform target)
        {
            m_ObservableTarget = target;
            m_StartTargetAngle = NormalizeAngle(m_ObservableTarget.eulerAngles.y);
            m_StartRotoAngle = m_RotoData.Angle;

            if (m_TargetRoutine != null)
            {
                behaviour.StopCoroutine(m_TargetRoutine);
                m_TargetRoutine = null;
            }

            m_TargetRoutine = behaviour.StartCoroutine(FollowTargetRoutine());
        }

        /// <summary>
        /// Start head tracking routine
        /// </summary>
        /// <param name="target">Target headset representation</param>
        /// <param name="behaviour">Target that will be used as the rotation preference.</param>
        public void StartHeadTracking(MonoBehaviour behaviour, Transform target)
        {
            m_ObservableTarget = target;
            m_StartTargetAngle = NormalizeAngle(m_ObservableTarget.eulerAngles.y);
            m_StartRotoAngle = m_RotoData.Angle;

            if (m_TargetRoutine != null)
            {
                behaviour.StopCoroutine(m_TargetRoutine);
                m_TargetRoutine = null;
            }

            m_TargetRoutine = behaviour.StartCoroutine(HeadTrackingRoutine());
        }

        /// <summary>
        /// Stop routine
        /// </summary>
        internal void StopRoutine(MonoBehaviour behaviour)
        {
            if (m_TargetRoutine != null)
            {
                behaviour.StopCoroutine(m_TargetRoutine);
                m_TargetRoutine = null;
                m_ObservableTarget = null;
            }
        }
#else
        

        /// <summary>
        /// Follow rotation of a target object
        /// </summary>
        /// <param name="behaviour">Target that will be used as the rotation preference.</param>
        /// <param name="targetFunc">Target function which returns a rotation to follow</param>
        public void FollowTarget(RotoBehaviour behaviour, Func<float?> targetFunc, bool joystickMode = false)
        {
            

            //var targetAngle = GetTargetAngle();

            //m_StartTargetAngle = NormalizeAngle(targetAngle);
            //m_StartRotoAngle = m_RotoData.Angle;
            
            if (m_CancelSource != null && !m_CancelSource.IsCancellationRequested)
            {
                m_CancelSource.Cancel(); 
                //wait for m_CancelSource to be cancelled
                
            }
            
            m_CancelSource = new CancellationTokenSource();

            // Start a background thread named FollowTargetRoutine pointing to FollowTargetRoutine async function passing the cancellation token
            m_ObservableTarget = targetFunc;

            

            var t = new Thread(async () =>
            {
                try
                {
                    if(joystickMode)
                        await ContinuousMovementRoutine(m_CancelSource.Token);
                    else
                        await FollowTargetRoutine(m_CancelSource.Token);
                }
                catch (Exception ex)
                {
                    // Log or handle the exception
                    Console.WriteLine($"Error in FollowTargetRoutine: {ex.Message}");
                }
            })
            {
                Name = "FollowTargetRoutine",
                IsBackground = true
            };

            t.Start();

        }

        /// <summary>
        /// Stop routine
        /// </summary>
        internal void StopRoutine(RotoBehaviour behaviour)
        {
            if (m_CancelSource != null && !m_CancelSource.IsCancellationRequested)
            {
                m_CancelSource.Cancel();
            }
        }
#endif

            /// <summary>
            /// Plays a rumble effect on the chair with a specified duration and power.
            /// </summary>
            /// <param name="duration">The duration of the rumble in seconds.</param>
            /// <param name="power">The power of the rumble (valid range is 0-100).</param>
        public void Rumble(float duration, int power)
        {
#if !UNITY_EDITOR && !NO_UNITY
            var rumbleModel = new RumbleModel(duration, power);
            SendMessage(new PlayRumbleMessage(rumbleModel.ToJson()));
#else
            if (m_ConnectionType == ConnectionType.Chair)
            {
                UsbConnector.Instance.PlayRumble(new RumbleModel(duration, power));
            }
#endif
        }

#if !NO_UNITY
        IEnumerator FollowTargetRoutine()
        {
            if (m_ObservableTarget == null)
                Debug.LogError("For FollowObject Mode you need to set target transform");
            else
            {
                float deltaTime = 0;

                yield return new WaitForSeconds(0.5f);
                var modeParams = new ModeParams
                {
                    CockpitAngleLimit = 30,
                    MaxPower = 100
                };

                SetMode(ModeType.HeadTrack, modeParams);

                while (true)
                {
                    deltaTime += Time.deltaTime;

                    if (deltaTime > 0.01f)
                    {
                        var currentAngle = (int)m_ObservableTarget.eulerAngles.y;
                        var angle = currentAngle - m_StartTargetAngle;

                        if (angle != 0)
                        {
                            angle = NormalizeAngle(angle);

                            var rotoAngle = (int)(m_StartRotoAngle + angle);
                            rotoAngle = NormalizeAngle(rotoAngle);

                            var delta = Mathf.Abs(rotoAngle - m_RotoData.Angle);

                            if (delta > 2)
                                RotateToAngle(Direction.Left, rotoAngle, 30);
                        }

                        deltaTime = 0;
                    }

                    yield return null;
                }
            }
        }

        IEnumerator HeadTrackingRoutine()
        {
            if (m_ObservableTarget == null)
                Debug.LogError("For Had Tracking Mode you need to set target transform");
            else
            {
                var lastTargetAngle = NormalizeAngle(m_ObservableTarget.eulerAngles.y);

                float deltaTime = 0;
                while (true)
                {
                    yield return null;
                    deltaTime += Time.deltaTime;

                    if (deltaTime > 0.1f)
                    {
                        var currentTargetAngle = NormalizeAngle(m_ObservableTarget.eulerAngles.y);
                        float currentRotoAngle = m_RotoData.Angle;

                        var direction = GetDirection((int)currentTargetAngle, (int)lastTargetAngle);

                        var deltaTargetAngle = GetDelta(m_StartTargetAngle, currentTargetAngle, direction);
                        var deltaRotoAngle = GetDelta(m_StartRotoAngle, currentRotoAngle, direction);

                        var angle = deltaTargetAngle - deltaRotoAngle;

                        angle = NormalizeAngle(angle);
                        angle += m_RotoData.Angle;

                        RotateToAngle(Direction.Left, (int)NormalizeAngle(angle), 30);
                        deltaTime = 0;

                        lastTargetAngle = currentTargetAngle;
                    }
                }
            }
        }

#else
        public static double MapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin);
        }

        public static double EnsureMapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return Math.Max(Math.Min(MapRange(x, xMin, xMax, yMin, yMax), Math.Max(yMin, yMax)), Math.Min(yMin, yMax));
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        struct Telemetry
        {
            public int ActualAngle;

            public int TargetAngle;

            public int Power;

            public float Delta;

            public int AntiJump;

            public float AngularVelocity;

            public float AvgTargetAngle;

            public float PreciseAngle;

            public float RecieveFPS;

            public float LerpedFPS;

            public float MaxPower;

            public float SendFPS;

            public int Direction;

            public int StopAngle;

            public int MinPower;
        }

        public struct SixDofTracker
        {
            public double sway, surge, heave, yaw, roll, pitch;
        }

        long m_AntiJump = 0;
        
        private float? GetTargetAngle()
        {
            
            if (m_ObservableTarget != null)
            {
                var targetAngle = m_ObservableTarget();
                if (targetAngle == null)
                {
                    m_StartTargetAngle = null;
                }
                else if (m_StartTargetAngle == null) // enable follow mode
                {
                    m_StartTargetAngle = targetAngle;
                    m_AntiJump = 0;
                    m_StartRotoAngle = m_RotoData.Angle;

                }
                else if (m_AntiJump > 100)
                {
                    m_StartTargetAngle = null;
                    m_AntiJump = 0; 
                }
                return targetAngle;
            }

            
            return null;
        }

        //object testObj = new object();
        Telemetry telemetry = new Telemetry();
        SixDofTracker oXRMC = new SixDofTracker();
#if MMF
        MmfTelemetry<Telemetry> tel = new (new() { Name = "RotoVR", Create = true });
#else
        UdpTelemetry<Telemetry> tel = new(new() { SendAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 16969) });
#endif

        MmfTelemetry<SixDofTracker> mComp = new(new("SimRacingStudioMotionRigPose", true));



        int sendFps = 50;
        int m_homeAngle = 0;
        int m_prevTargetAngle = 0;

        async Task FollowTargetRoutine(CancellationToken cancellationToken)
        {
            if (m_ObservableTarget == null)
                Debug.LogError("For FollowObject Mode you need to set target func");
            else
            {
                await Task.Delay(500);                
                m_yawInterpolator.OnValueUpdate += M_yawInterpolator_OnAngleUpdate;
                m_yawInterpolator.Start(90, cancellationToken);
                
                int targetMs = 1000 / sendFps;

                telemetry.MinPower = 30;
                m_homeAngle = m_RotoData.Angle;

                var sendWatch = Stopwatch.StartNew();                

                while (!cancellationToken.IsCancellationRequested)
                {
                    var currentTargetAngle = GetTargetAngle();

                    if (currentTargetAngle != null && m_StartTargetAngle != null)
                    {

                        var deltaTargetAngle = currentTargetAngle.Value - m_StartTargetAngle.Value;

                        if ((int)deltaTargetAngle != 0 )
                        {
                            deltaTargetAngle = NormalizeAngle(deltaTargetAngle);

                            var targetRotoAngle = NormalizeAngle(m_StartRotoAngle + deltaTargetAngle);

                            var avgTargetRotoAngle = 0f;// CalcAvg(targetRotoAngle);

                            
                            var fDelta = Math.Abs(targetRotoAngle - m_RotoData.Angle);
                            if (fDelta > 180)                            
                                fDelta = 360 - fDelta;
                            
                            

                            if (fDelta >= 1)
                            {
                                m_AntiJump = 0;
                                
                                var maxPower = 60;
                                var brakePoint = 10;// MaxPower <= 80 ? 10 : 60;
                                var pmin = 20;
                                var pmax = 30;
                                int power = maxPower;
                                /*
                                if (telemetry.Power > 40 && telemetry.MinPower == pmax)
                                {
                                    telemetry.MinPower = pmin;
                                    power = telemetry.Power;
                                }
                                else if (telemetry.Power < 22 && telemetry.MinPower == pmin)
                                {
                                    telemetry.MinPower = pmax;
                                    power = telemetry.Power;
                                }
                                else
                                {
                                    power = (int)EnsureMapRange(fDelta, 0, brakePoint, telemetry.MinPower, maxPower);
                                }*/

                                telemetry.MaxPower = Math.Max(telemetry.MaxPower, power);
                                telemetry.Power = power;

                                var dir = (int)GetDirection((int)targetRotoAngle, (int)telemetry.AvgTargetAngle);
                                
                                telemetry.Direction =  dir * 10;
                                telemetry.AvgTargetAngle = avgTargetRotoAngle;
                                telemetry.Delta = (int)fDelta;
                                telemetry.AntiJump = (int)m_AntiJump;

                                //if(fDelta < 1 && fDelta > 0.1)
                                //{
                                //    targetRotoAngle = (int)NormalizeAngle(fDelta + 1 *( dir == 0 ? 1 : -1));
                                //}

                                RotateToAngle(Direction.Right, (int)targetRotoAngle, power);
                            }
                            else // less than one degree of movement to you are aligned so increment the anti jump
                            {                                
                                m_AntiJump = sendWatch.ElapsedMilliseconds;
                            }
                        }

                        m_prevTargetAngle = (int)currentTargetAngle;
                    }
                    
                    var elapsedTimeLeft = targetMs - sendWatch.ElapsedMilliseconds;
                    
                    if ((int)elapsedTimeLeft > 0)
                    {
                        SleepAccurate(elapsedTimeLeft);
                        //Thread.Sleep((int)elapsedTimeLeft);
                        //await Task.Delay((int)elapsedTimeLeft);
                    }

                    telemetry.SendFPS = 1000f / Math.Max(1, sendWatch.ElapsedMilliseconds);
                    sendWatch.Restart();
                    
                }
                                
                m_ObservableTarget = null;
            }
        }

        EnforcedQueue<float> m_xQ = new(3);
        const float TOP_SPEED_DEG_PER_MS = 120 / 1000f;

        async Task ContinuousMovementRoutine(CancellationToken cancellationToken)
        {
            if (m_ObservableTarget == null)
                Debug.LogError("For FollowObject Mode you need to set target func");
            else
            {
                await Task.Delay(500);
                m_yawInterpolator.OnValueUpdate += M_yawInterpolator_OnAngleUpdate;
                m_yawInterpolator.Start(90, cancellationToken);

                int targetMs = 1000 / sendFps;


                
                var brakePoint = 45; // maxPower <= 80 ? 50 : 60;
                var degreesPerFrame = TOP_SPEED_DEG_PER_MS * targetMs + brakePoint;

                float prevX = 0f;
                //var outfrac = (maxPower - 30f) + (( (maxPower - 24f) - (maxPower - 30f)) / (maxPower - 30f));
                var sendWatch = Stopwatch.StartNew();

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (m_ObservableTarget != null)
                    {
                        var x = m_ObservableTarget();
                        
                        
                        if (x == null )
                        {
                            m_StartTargetAngle = null;
                        }
                        else
                        {
                            
                            if(Math.Abs(x.Value) > 1)
                                x = (float)EnsureMapRange(x.Value, -1, 1, -1, 1);
                            
                            m_xQ.Enqueue(x.Value);

                            telemetry.Delta = Math.Abs(x.Value - prevX);
                            telemetry.MaxPower = 60;
                            telemetry.MinPower = 30;
                            //var outward = Math.Abs(x.Value) - Math.Abs(prevX);
                            //var om = outward >= 0 ? 30 : 20;
                            
                            //var outRange = maxPower - outMin;
                            //bool m = x.Value > .8;
                            telemetry.AvgTargetAngle = x.Value;// m_xQ.Average();
                            telemetry.Power = 60; //(int) EnsureMapRange(Math.Abs(telemetry.AvgTargetAngle), 0.3f, 1f, telemetry.MinPower, telemetry.MaxPower);
                            telemetry.Direction = Math.Sign(telemetry.AvgTargetAngle) * 10;

                            
                            var offset = degreesPerFrame * Math.Sign(telemetry.AvgTargetAngle);
                            
                            var ang = NormalizeAngle(m_RotoData.Angle + offset);
                            
                            
                            
                            telemetry.TargetAngle = (int) ang; //Math.Abs(x.Value) > 0.07 ? (int)ang : 0;
                            //telemetry.Direction = direction;
                            
                            RotateToAngle(Direction.Left, telemetry.TargetAngle, telemetry.Power);
                            prevX = x.Value;
                        }


                    }



                    //telemetry.MaxPower = Math.Max(telemetry.MaxPower, telemetry.Power);



                    var elapsedTimeLeft = targetMs - sendWatch.ElapsedMilliseconds;
                    SleepAccurate(elapsedTimeLeft);
                    if (sendWatch.ElapsedMilliseconds != 0)
                        telemetry.SendFPS = 1000f / Math.Max(1, sendWatch.ElapsedMilliseconds);
                    else
                        telemetry.SendFPS = 0;

                    sendWatch.Restart();
                }

                sendWatch.Stop();
                await SlowDownAndStop();
                m_ObservableTarget = null;


            }

        }

       

        private Task<int> SlowDownAndStop()
        {
            return Task.Run(() =>
            {
                var pwr = telemetry.Power - 5;
                if (telemetry.Power > 30)
                {
                    var s = Stopwatch.StartNew();
                    for (float i = pwr; i > 20; i = i * .80f)
                    {
                        RotateToAngle(Direction.Right, m_RotoData.Angle + ((int)(TOP_SPEED_DEG_PER_MS * 66)), pwr);
                        Thread.Sleep(66 - (int)s.ElapsedMilliseconds);
                        s.Restart();
                    }

                    s.Stop();

                    return Task.FromResult((int)s.ElapsedMilliseconds);
                }
                else
                {
                    telemetry = new();
                }

                return Task.FromResult(0);
            });
        }
        private void SleepAccurate(float ms)
        {
            if (ms <= float.Epsilon)
                return;

            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalMilliseconds < ms)
            {
                Thread.SpinWait(1);
            }
            stopwatch.Stop();
        }

        private void M_yawInterpolator_OnAngleUpdate(float angle)
        {
            //float previousAngle = m_Queue.LastOrDefault().angle;
            var es = _angleUpdateStopwatch.ElapsedMilliseconds == 0 ? 1 : _angleUpdateStopwatch.ElapsedMilliseconds;
            //var av = Math.Abs(angle - previousAngle) /es ;

            m_Queue.Enqueue((_angleUpdateStopwatch.ElapsedMilliseconds, angle));
            _angleUpdateStopwatch.Restart();

            telemetry.ActualAngle = m_RotoData.Angle;
            telemetry.AngularVelocity = CalculateAngularVelocity();
            telemetry.PreciseAngle = angle;
            telemetry.RecieveFPS = m_yawInterpolator.OriginalFramerate;
            telemetry.LerpedFPS = es <= 1 ?  0 : 1000 / es;// m_yawInterpolator.TargetFramerate;
            tel.Send(telemetry);
            
            oXRMC.yaw = -NormalizeAngle(angle - m_homeAngle);

            mComp.Send(oXRMC);
        }


#endif

        double CalcAvg(float angle)
        {
            m_directions.Enqueue(AngleToDirectionVector(angle));

            var avgVector = m_directions.CalculateAverage(((double x, double y) acc, (double x, double y) curr) =>
            {
                return (acc.x + curr.x, acc.y + curr.y);
            }, (sum) =>
            {
                var mag = Math.Sqrt(sum.x * sum.x + sum.y * sum.y);
                var avg = (sum.x / mag, sum.y / mag);
                return avg;
            });

            return NormalizeAngle((float)(Math.Atan2(avgVector.y, avgVector.x) * (180 / Math.PI))); 
        }

        double CalculateAverage(IEnumerable<(double x, double y)> values)
        {


            double sumX = 0;
            double sumY = 0;
            foreach (var value in values)
            {
                sumX += value.x;
                sumY += value.y;
            }

            //normalize the sum
            double length = Math.Sqrt(sumX * sumX + sumY * sumY);

            if (length > 0)
            {
                sumX /= length;
                sumY /= length;
            }

            return NormalizeAngle((float)(Math.Atan2(sumY, sumX) * (180 / Math.PI)));
        }

        (double x, double y) AngleToDirectionVector(float degrees)
        {
            return new(Math.Cos(degrees * Math.PI / 180), Math.Sin(degrees * Math.PI / 180));
        }

        float GetDelta(float startAngle, float currentAngle, Direction direction)
        {
            float delta = 0;

            switch (direction)
            {
                case Direction.Left:
                    if (currentAngle < startAngle)
                        delta = currentAngle - startAngle;
                    else
                    {
                        delta = currentAngle - startAngle - 360;
                    }

                    break;
                case Direction.Right:
                    if (currentAngle > startAngle)
                        delta = currentAngle - startAngle;
                    else
                    {
                        delta = currentAngle - startAngle + 360;
                    }

                    break;
            }

            return delta;
        }

        Direction GetDirection(int targetAngle, int sourceAngle)
        {
            if (targetAngle > sourceAngle)
            {
                if (Math.Abs(targetAngle - sourceAngle) > 180)
                {
                    return Direction.Left;
                }
                else
                {
                    return Direction.Right;
                }
            }
            else
            {
                if (Math.Abs(targetAngle - sourceAngle) > 180)
                {
                    return Direction.Right;
                }
                else
                {
                    return Direction.Left;
                }
            }
        }

        float NormalizeAngle(float angle)
        {
            if (angle < 0)
                angle += 360;
            else if (angle > 360)
                angle -= 360;

            return angle;
        }

        int NormalizeAngle(int angle)
        {
            if (angle < 0)
                angle += 360;
            else if (angle > 360)
                angle -= 360;

            return angle;
        }
        
    }
}