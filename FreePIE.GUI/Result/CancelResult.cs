﻿using Caliburn.Micro;
using Action = System.Action;

namespace FreePIE.GUI.Result
{
    public class CancelResult : Result
    {
        private readonly Action cancelCallback;

        public CancelResult(System.Action cancelCallback)
        {
            this.cancelCallback = cancelCallback;
        }

        public override void Execute(Caliburn.Micro.ActionExecutionContext context)
        {
            if (cancelCallback != null)
                cancelCallback();

            OnCompleted(this, new ResultCompletionEventArgs { WasCancelled = true });
        }
    }
}
