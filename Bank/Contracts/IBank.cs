﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IBank
    {
        [OperationContract]
        [FaultContract(typeof(BankException))]
        void TestCommunication();

        [OperationContract]
        [FaultContract(typeof(BankException))]
        void Deposit(byte[] message);
    }
}
