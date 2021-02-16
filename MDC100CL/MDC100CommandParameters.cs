using System;

namespace MDC100CL
{
    public class MDC100CommandParameters
    {
        private int _bytesReturned = 0;
        private int _msDelay = 0;

        // Constructor
        public MDC100CommandParameters()
        {
            _bytesReturned = 2;
            _msDelay = 20;
        }
        // Constructor

        public MDC100CommandParameters(int bytesReturned, int nodeSerialNumber)
        {
            _bytesReturned = bytesReturned;
            _msDelay = nodeSerialNumber;
        }

        public override string ToString()
        {
            return string.Format($"bytes: {BytesReturned}, delay: {MsDelay}");
        }


        public int BytesReturned => this._bytesReturned;

        public int MsDelay => this._msDelay;

    }
}