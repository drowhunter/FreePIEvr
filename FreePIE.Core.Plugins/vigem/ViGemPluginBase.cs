using System;
using System.Collections.Generic;
using System.Linq;

using FreePIE.Core.Contracts;

using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;

namespace FreePIE.Core.Plugins.vigem
{

    public abstract class ViGemPluginBase : Plugin
    {
        /// <summary>
        /// Indicates what went wrong
        /// </summary>
        public enum ErrorState
        {
            OK = 0,
            OPEN_FAILED,
            CLOSE_FAILED,
            PLUG_FAILED,
            UNPLUG_FAILED,
        }

        protected List<VigemGlobalBase> _globals;
        public ViGEmClient Client { get; private set; }

        private List<int> _connectedControllers = new List<int>();
        /// <summary>
        /// global flips this if an error occured plugging in
        /// </summary>
        private ErrorState _errorOccured;

        private bool IsConnected
        {
            get
            {
                if (Client != null)
                    return true;

                return false;
            }
        }

        public override Action Start()
        {
            try
            {
                Client = new ViGEmClient();
                
            }
            catch (Exception x)
            {
                _errorOccured = ErrorState.OPEN_FAILED;
                throw new Exception("You must install the ViGEM Virtual Bus driver. See https://github.com/nefarius/ViGEm/wiki/Driver-Installation");
            }

            return base.Start();
        }

        public override void Stop()
        {
            _globals.ForEach(g =>
            {
                g.Disconnect();
                UnPlugController(g.index);
            });

            _connectedControllers.Clear();
        }

        public override void DoBeforeNextExecute()
        {
            _globals.ForEach(d => d.Update());
        }

        protected void PlugController(int index)
        {
            if (!IsConnected || _errorOccured != ErrorState.OK)
                return;

            if (!_connectedControllers.Contains(index))
            {
                _connectedControllers.Add(index);
            }
        }

        protected void UnPlugController(int index)
        {
            if (!IsConnected || _errorOccured != ErrorState.OK)
                return;

            if (_connectedControllers.Contains(index))
            {
                _connectedControllers.Remove(index);
            }
        }
    }

    public abstract class VigemGlobalBase 
    {
        

        public readonly int index = -1;
        protected List<VigemGlobalBase> _globals;
        

        public VigemGlobalBase(int index)
        {
            
            this.index = index;

        }

        internal abstract void Update();

        protected bool isBetween(double val, double min, double max, bool isInclusive = true)
        {
            if (isInclusive)
            {
                return (val >= min) && (val <= max);
            }
            else
            {
                return (val > min) && (val < max);
            }
        }

        protected double mapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return yMin + (yMax - yMin) * (x - xMin) / (xMax - xMin);
        }

        protected double ensureMapRange(double x, double xMin, double xMax, double yMin, double yMax)
        {
            return Math.Max(Math.Min(((x - xMin) / (xMax - xMin)) * (yMax - yMin) + yMin, Math.Max(yMin, yMax)), Math.Min(yMin, yMax));
        }

        internal abstract void Disconnect();

        
    }


    
}
