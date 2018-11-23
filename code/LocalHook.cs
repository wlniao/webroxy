using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TcpRouter
{
    /// <summary>
    /// 本地的webhook信息
    /// </summary>
    public class LocalHook
    {
        /// <summary>
        /// 工作目录
        /// </summary>
        public string workdir { get; set; }
        /// <summary>
        /// Push时间
        /// </summary>
        public string pushtime { get; set; }
        /// <summary>
        /// Pull时间
        /// </summary>
        public string pulltime { get; set; }
    }
}
