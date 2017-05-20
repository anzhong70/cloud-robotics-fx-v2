using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRoboSpeech
{
    public class CRoboMessage
    {
        public object RbHeader { set; get; }
        public object RbBody { set; get; }
    }

    public class CRbHeader
    {
        public string RoutingType { set; get; }
        public string RoutingKeyword { set; get; }
        public string AppId { set; get; }
        public string AppProcessingId { set; get; }
        public string MessageId { set; get; }
        public string MessageSeqno { set; get; }
        public string SendDateTime { set; get; }
    }

    public class CRbBodyBasic
    {
        public string visitor { set; get; }
    }

    public class CRbBodyVisionAnalyze
    {
        public string visitor { set; get; }
        public string visitor_id { set; get; }
        public string blobFileName { set; get; }
        public string deleteFile { set; get; }
    }

    public class CRbBodyBlobData
    {
        public string blobFileName { set; get; }
        public string storageAccount { set; get; }
        public string storageKey { set; get; }
        public string storageContainer { set; get; }
    }
}
