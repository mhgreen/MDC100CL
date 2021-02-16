using System;

namespace MDC100CL
{
    public class FTDIPort
    {
        private string _nodeComportName = "";
        private string _nodeDescription = "";
        private string _nodeSerialNumber = "";

        // Constructor
        public FTDIPort()
        {
            _nodeComportName = "";
            _nodeDescription = "";
            _nodeSerialNumber = "";
        }
        // Constructor

        public FTDIPort(string nodeComportName, string nodeDescription, string nodeSerialNumber)
        {
            _nodeComportName = nodeComportName;
            _nodeDescription = nodeDescription;
            _nodeSerialNumber = nodeSerialNumber;
        }

        public string NodeComportName => this._nodeComportName;

        public string NodeDescription => this._nodeDescription;

        public string NodeSerialNumber => this._nodeSerialNumber;

    }
}