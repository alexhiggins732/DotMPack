using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HigginsSoft.DotMPack;
using System.ServiceModel;
using System.Runtime.Serialization;
namespace igginsSoft.DotMPack.GrpcTests
{
    internal class PackMethodTests
    {

        [ServiceContract]
        public interface IConfigurationService
        {
            Pack GetValue(Pack request);
            Pack SetValue(Pack request);

        }
    }
}

