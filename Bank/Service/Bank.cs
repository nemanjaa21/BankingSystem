﻿using Contracts;
using Manager;
using Service.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class Bank : IBank
    {
        public void Deposit(byte[] message)
        {
            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);

            string secretKey = SecretKey.LoadKey(clientName);

            byte[] decrypted = TripleDES.Decrypt(message, secretKey);

            byte[] sign = new byte[256];
            byte[] body = new byte[decrypted.Length - 256];

            Buffer.BlockCopy(decrypted, 0, sign, 0, 256);
            Buffer.BlockCopy(decrypted, 256, body, 0, decrypted.Length - 256);

            string decryptedMessage = System.Text.Encoding.UTF8.GetString(body);

            X509Certificate2 signCert =
                CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, clientName + "_sign");

            if (DigitalSignature.Verify(decryptedMessage, sign, signCert))
            {
                string pin = decryptedMessage.Split('-')[0];
                string amount = decryptedMessage.Split('-')[1];

                List<Racun> racuni = XMLHelper.ReadAllBankAccounts();

                var racun = racuni.Find(x => x.Username.Equals(clientName));

                if (racun.Pin.Equals(HashHelper.HashPassword(pin)))
                {
                    float floatAmount = 0;

                    if (float.TryParse(amount, out floatAmount))
                    {
                        XMLHelper.UpdateBankAccountBalance(clientName, floatAmount);
                    }
                    else
                    {
                        throw new FaultException<BankException>(
                            new BankException("Kolicina za uplatu mora biti broj."));
                    }

                }
                else
                {
                    throw new FaultException<BankException>(
                        new BankException("Pogresan pin."));
                }
            }
            else
            {
                throw new FaultException<BankException>(
                    new BankException("Potpis nije validan."));
            }
        }

        public void Withdraw(byte[] message)
        {
            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);

            string secretKey = SecretKey.LoadKey(clientName);

            byte[] decrypted = TripleDES.Decrypt(message, secretKey);

            byte[] sign = new byte[256];
            byte[] body = new byte[decrypted.Length - 256];

            Buffer.BlockCopy(decrypted, 0, sign, 0, 256);
            Buffer.BlockCopy(decrypted, 256, body, 0, decrypted.Length - 256);

            string decryptedMessage = System.Text.Encoding.UTF8.GetString(body);

            X509Certificate2 signCert =
                CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, clientName + "_sign");

            if (DigitalSignature.Verify(decryptedMessage, sign, signCert))
            {
                string pin = decryptedMessage.Split('-')[0];
                string amount = decryptedMessage.Split('-')[1];

                List<Racun> racuni = XMLHelper.ReadAllBankAccounts();

                var racun = racuni.Find(x => x.Username.Equals(clientName));

                if (racun.Pin.Equals(HashHelper.HashPassword(pin)))
                {
                    float floatAmount = 0;

                    if (float.TryParse(amount, out floatAmount))
                    {
                        if (racun.Balance - floatAmount >= 0)
                        {
                            XMLHelper.UpdateBankAccountBalance(clientName, -floatAmount);
                        }
                        else
                        {
                            throw new FaultException<BankException>(
                                new BankException("Nemate dovoljno sredstava na racunu."));
                        }
                    }
                    else
                    {
                        throw new FaultException<BankException>(
                            new BankException("Kolicina za uplatu mora biti broj."));
                    }

                }
                else
                {
                    throw new FaultException<BankException>(
                        new BankException("Pogresan pin."));
                }
            }
            else
            {
                throw new FaultException<BankException>(
                    new BankException("Potpis nije validan."));
            }
        }

        public void TestCommunication()
        {
            Console.WriteLine("[TRANSACTION] Communication established.");
        }

        public byte[] ResetPin(byte[] message)
        {
            byte[] encrypted = null;

            string clientName = Formatter.ParseName(ServiceSecurityContext.Current.PrimaryIdentity.Name);

            string secretKey = SecretKey.LoadKey(clientName);

            byte[] decrypted = TripleDES.Decrypt(message, secretKey);

            byte[] sign = new byte[256];
            byte[] body = new byte[decrypted.Length - 256];

            Buffer.BlockCopy(decrypted, 0, sign, 0, 256);
            Buffer.BlockCopy(decrypted, 256, body, 0, decrypted.Length - 256);

            string oldPin = System.Text.Encoding.UTF8.GetString(body);

            X509Certificate2 signCert =
                CertManager.GetCertificateFromStorage(StoreName.TrustedPeople, StoreLocation.LocalMachine, clientName + "_sign");

            if (DigitalSignature.Verify(oldPin, sign, signCert))
            {

                List<Racun> racuni = XMLHelper.ReadAllBankAccounts();

                var racun = racuni.Find(x => x.Username.Equals(clientName));

                if (racun.Pin.Equals(HashHelper.HashPassword(oldPin)))
                {
                    string newPin = PinHelper.GeneratePin();

                    byte[] pinBuffer = System.Text.Encoding.UTF8.GetBytes(newPin);

                    XMLHelper.UpdateBankAccount(clientName, HashHelper.HashPassword(newPin));

                    X509Certificate2 signBank =
                        CertManager.GetCertificateFromStorage(StoreName.My, StoreLocation.LocalMachine, "bank_sign");

                    byte[] signedMessage = DigitalSignature.Create(newPin, signBank);

                    byte[] plaintext = new byte[256 + pinBuffer.Length];

                    Buffer.BlockCopy(signedMessage, 0, plaintext, 0, 256);
                    Buffer.BlockCopy(pinBuffer, 0, plaintext, 256, pinBuffer.Length);

                    encrypted = TripleDES.Encrypt(plaintext, secretKey);

                    return encrypted;
                }
                else
                {
                    throw new FaultException<BankException>(
                        new BankException("Stari pin je pogresan."));
                }
            }
            else
            {
                throw new FaultException<BankException>(
                    new BankException("Potpis nije validan."));
            }
        }
    }
}
