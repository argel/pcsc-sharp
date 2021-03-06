﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PCSC.Interop.Windows
{
    /// <summary>
    /// PC/SC API for Microsoft Win32/Win64 (x86/x64/IA64) 
    /// </summary>
    internal sealed class WinSCardAPI : ISCardAPI
    {
        private const int MAX_READER_NAME = 255;
        private const string WINSCARD_DLL = "winscard.dll";
        private const string KERNEL_DLL = "KERNEL32.DLL";
        private const int CHARSIZE = sizeof(char);
        
        public const int MAX_ATR_SIZE = 0x24;

        private IntPtr _dllHandle = IntPtr.Zero;
        private Encoding _textEncoding;

        public int MaxAtrSize {
            get { return MAX_ATR_SIZE; }
        }

        public Encoding TextEncoding {
            get { return _textEncoding; }
            set { _textEncoding = value; }
        }

        public int CharSize {
            get { return CHARSIZE; }
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardEstablishContext(
            [In] Int32 dwScope,
            [In] IntPtr pvReserved1,
            [In] IntPtr pvReserved2,
            [In, Out] ref IntPtr phContext);

        public SCardError EstablishContext(SCardScope dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext) {
            var ctx = IntPtr.Zero;
            var rc = SCardHelper.ToSCardError(
                SCardEstablishContext(
                    (Int32) dwScope,
                    pvReserved1,
                    pvReserved2,
                    ref ctx));
            phContext = ctx;
            return rc;
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardReleaseContext(
            [In] IntPtr hContext);

        public SCardError ReleaseContext(IntPtr hContext) {
            return SCardHelper.ToSCardError(SCardReleaseContext(hContext));
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardIsValidContext(
            [In] IntPtr hContext);

        public SCardError IsValidContext(IntPtr hContext) {
            return SCardHelper.ToSCardError(SCardIsValidContext(hContext));
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardListReaders(
            [In] IntPtr hContext,
            [In] byte[] mszGroups,
            [Out] byte[] pmszReaders,
            [In, Out] ref Int32 pcchReaders);

        public SCardError ListReaders(IntPtr hContext, string[] groups, out string[] readers) {
            var dwReaders = 0;

            // initialize groups array
            byte[] mszGroups = null;
            if (groups != null)
                mszGroups = SCardHelper.ConvertToByteArray(groups, _textEncoding);

            // determine the needed buffer size
            var rc = SCardHelper.ToSCardError(
                SCardListReaders(hContext,
                    mszGroups,
                    null,
                    ref dwReaders));

            if (rc != SCardError.Success) {
                readers = null;
                return rc;
            }

            // initialize array
            byte[] mszReaders = new byte[dwReaders * sizeof(char)];

            rc = SCardHelper.ToSCardError(
                SCardListReaders(hContext,
                    mszGroups,
                    mszReaders,
                    ref dwReaders));

            readers = (rc == SCardError.Success)
                ? SCardHelper.ConvertToStringArray(mszReaders, _textEncoding)
                : null;

            return rc;
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardListReaderGroups(
            [In] IntPtr hContext,
            [Out] byte[] mszGroups,
            [In, Out] ref Int32 pcchGroups);

        public SCardError ListReaderGroups(IntPtr hContext, out string[] groups) {
            var dwGroups = 0;

            // determine the needed buffer size
            var rc = SCardHelper.ToSCardError(
                SCardListReaderGroups(
                    hContext,
                    null,
                    ref dwGroups));

            if (rc != SCardError.Success) {
                groups = null;
                return rc;
            }

            // initialize array
            byte[] mszGroups = new byte[dwGroups * sizeof(char)];

            rc = SCardHelper.ToSCardError(
                SCardListReaderGroups(
                    hContext,
                    mszGroups,
                    ref dwGroups));

            groups = (rc == SCardError.Success)
                ? SCardHelper.ConvertToStringArray(mszGroups, _textEncoding)
                : null;

            return rc;
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardConnect(
            [In] IntPtr hContext,
            [In] byte[] szReader,
            [In] Int32 dwShareMode,
            [In] Int32 dwPreferredProtocols,
            [Out] out IntPtr phCard,
            [Out] out Int32 pdwActiveProtocol);

        public SCardError Connect(IntPtr hContext, string szReader, SCardShareMode dwShareMode, SCardProtocol dwPreferredProtocols, out IntPtr phCard, out SCardProtocol pdwActiveProtocol) {
            var readername = SCardHelper.ConvertToByteArray(szReader, _textEncoding, Platform.Lib.CharSize);
            Int32 activeproto;

            var result = SCardConnect(hContext,
                readername,
                (Int32) dwShareMode,
                (Int32) dwPreferredProtocols,
                out phCard,
                out activeproto);

            pdwActiveProtocol = (SCardProtocol) activeproto;

            return SCardHelper.ToSCardError(result);
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardReconnect(
            [In] IntPtr hCard,
            [In] Int32 dwShareMode,
            [In] Int32 dwPreferredProtocols,
            [In] Int32 dwInitialization,
            [Out] out Int32 pdwActiveProtocol);

        public SCardError Reconnect(IntPtr hCard, SCardShareMode dwShareMode, SCardProtocol dwPreferredProtocols, SCardReaderDisposition dwInitialization, out SCardProtocol pdwActiveProtocol) {
            Int32 activeproto;
            var result = SCardReconnect(
                hCard,
                (Int32) dwShareMode,
                (Int32) dwPreferredProtocols,
                (Int32) dwInitialization,
                out activeproto);

            pdwActiveProtocol = (SCardProtocol) activeproto;
            return SCardHelper.ToSCardError(result);
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardDisconnect(
            [In] IntPtr hCard,
            [In] Int32 dwDisposition);

        public SCardError Disconnect(IntPtr hCard, SCardReaderDisposition dwDisposition) {
            return SCardHelper.ToSCardError(SCardDisconnect(hCard, (Int32) dwDisposition));
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardBeginTransaction(
            [In] IntPtr hCard);

        public SCardError BeginTransaction(IntPtr hCard) {
            return SCardHelper.ToSCardError(SCardBeginTransaction(hCard));
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardEndTransaction(
            [In] IntPtr hCard,
            [In] Int32 dwDisposition);

        public SCardError EndTransaction(IntPtr hCard, SCardReaderDisposition dwDisposition) {
            return SCardHelper.ToSCardError(SCardEndTransaction(hCard, (Int32) dwDisposition));
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardTransmit(
            [In] IntPtr hCard,
            [In] IntPtr pioSendPci,
            [In] byte[] pbSendBuffer,
            [In] Int32 cbSendLength,
            [In, Out] IntPtr pioRecvPci,
            [Out] byte[] pbRecvBuffer,
            [In, Out] ref Int32 pcbRecvLength);

        public SCardError Transmit(IntPtr hCard, IntPtr pioSendPci, byte[] pbSendBuffer, IntPtr pioRecvPci, byte[] pbRecvBuffer, out int pcbRecvLength) {
            pcbRecvLength = 0;
            if (pbRecvBuffer != null) {
                pcbRecvLength = pbRecvBuffer.Length;
            }

            var pcbSendLength = 0;
            if (pbSendBuffer != null) {
                pcbSendLength = pbSendBuffer.Length;
            }

            return Transmit(
                hCard,
                pioSendPci,
                pbSendBuffer,
                pcbSendLength,
                pioRecvPci,
                pbRecvBuffer,
                ref pcbRecvLength);
        }

        public SCardError Transmit(IntPtr hCard, IntPtr pioSendPci, byte[] pbSendBuffer, int pcbSendLength, IntPtr pioRecvPci, byte[] pbRecvBuffer, ref int pcbRecvLength) {
            var recvlen = 0;

            if (pbRecvBuffer != null) {
                if (pcbRecvLength > pbRecvBuffer.Length || pcbRecvLength < 0) {
                    throw new ArgumentOutOfRangeException("pcbRecvLength");
                }
                recvlen = pcbRecvLength;
            } else {
                if (pcbRecvLength != 0) {
                    throw new ArgumentOutOfRangeException("pcbRecvLength");
                }
            }

            var sendbuflen = 0;
            if (pbSendBuffer != null) {
                if (pcbSendLength > pbSendBuffer.Length || pcbSendLength < 0) {
                    throw new ArgumentOutOfRangeException("pcbSendLength");
                }
                sendbuflen = pcbSendLength;
            } else {
                if (pcbSendLength != 0) {
                    throw new ArgumentOutOfRangeException("pcbSendLength");
                }
            }

            var rc = SCardHelper.ToSCardError((SCardTransmit(
                hCard,
                pioSendPci,
                pbSendBuffer,
                sendbuflen,
                pioRecvPci,
                pbRecvBuffer,
                ref recvlen)));

            pcbRecvLength = recvlen;

            return rc;
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardControl(
            [In] IntPtr hCard,
            [In] Int32 dwControlCode,
            [In] byte[] lpInBuffer,
            [In] Int32 nInBufferSize,
            [In, Out] byte[] lpOutBuffer,
            [In] Int32 nOutBufferSize,
            [Out] out Int32 lpBytesReturned);

        public SCardError Control(IntPtr hCard, IntPtr dwControlCode, byte[] pbSendBuffer, byte[] pbRecvBuffer, out int lpBytesReturned) {
            var sendbuflen = 0;
            if (pbSendBuffer != null) {
                sendbuflen = pbSendBuffer.Length;
            }

            var recvbuflen = 0;
            if (pbRecvBuffer != null) {
                recvbuflen = pbRecvBuffer.Length;
            }

            Int32 bytesret;

            var rc = SCardHelper.ToSCardError(SCardControl(
                hCard,
                unchecked((Int32)dwControlCode.ToInt64()), // On a 64-bit platform IntPtr.ToInt32() will throw an OverflowException 
                pbSendBuffer,
                sendbuflen,
                pbRecvBuffer,
                recvbuflen,
                out bytesret));

            lpBytesReturned = bytesret;

            return rc;
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardStatus(
            [In] IntPtr hCard,
            [Out] byte[] szReaderName,
            [In, Out] ref Int32 pcchReaderLen,
            [Out] out Int32 pdwState,
            [Out] out Int32 pdwProtocol,
            [Out] byte[] pbAtr,
            [In, Out] ref Int32 pcbAtrLen);

        public SCardError Status(IntPtr hCard, out string[] szReaderName, out IntPtr pdwState, out IntPtr pdwProtocol, out byte[] pbAtr) {
            var readerName = new byte[MAX_READER_NAME * CharSize];
            var readerNameSize = MAX_READER_NAME;

            pbAtr = new byte[MAX_ATR_SIZE];
            var atrlen = pbAtr.Length;

            Int32 state, proto;

            var rc = SCardHelper.ToSCardError(SCardStatus(
                hCard,
                readerName,
                ref readerNameSize,
                out state,
                out proto,
                pbAtr,
                ref atrlen));

            if (rc == SCardError.InsufficientBuffer || (MAX_READER_NAME < readerNameSize) || (pbAtr.Length < atrlen)) {
                // second try
               
                if (MAX_READER_NAME < readerNameSize) {
                    // readername byte array was too short
                    readerName = new byte[readerNameSize * CharSize];
                }

                if (pbAtr.Length < atrlen) {
                    // ATR byte array was too short
                    pbAtr = new byte[atrlen];
                }

                rc = SCardHelper.ToSCardError(SCardStatus(
                    hCard,
                    readerName,
                    ref readerNameSize,
                    out state,
                    out proto,
                    pbAtr,
                    ref atrlen));
            }

            pdwState = (IntPtr)state;
            pdwProtocol = (IntPtr)proto;

            if (rc == SCardError.Success) {
                if (atrlen < pbAtr.Length) {
                    Array.Resize(ref pbAtr, atrlen);
                }

                if (readerNameSize < (readerName.Length / CharSize)) {
                    Array.Resize(ref readerName, readerNameSize * CharSize);
                }

                szReaderName = SCardHelper.ConvertToStringArray(readerName, _textEncoding);
            } else {
                szReaderName = null;
            }
            
            return rc;
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardGetStatusChange(
            [In] IntPtr hContext,
            [In] Int32 dwTimeout,
            [In, Out,
             MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] SCARD_READERSTATE[] rgReaderStates,
            [In] Int32 cReaders);

        public SCardError GetStatusChange(IntPtr hContext, IntPtr dwTimeout, SCardReaderState[] rgReaderStates) {
            SCARD_READERSTATE[] readerstates = null;
            var cReaders = 0;

            if (rgReaderStates != null) {
                // copy the last known state into the buffer
                cReaders = rgReaderStates.Length;
                readerstates = new SCARD_READERSTATE[cReaders];
                for (var i = 0; i < cReaders; i++) {
                    readerstates[i] = rgReaderStates[i].WindowsReaderState;
                }
            }

            // On a 64-bit platforms .ToInt32() will throw an OverflowException 
            var timeout = unchecked((Int32) dwTimeout.ToInt64());
            var rc = SCardHelper.ToSCardError(
                SCardGetStatusChange(
                    hContext,
                    timeout,
                    readerstates,
                    cReaders));

            if (rc != SCardError.Success || rgReaderStates == null) {
                return rc;
            }

            for (var i = 0; i < cReaders; i++) {
                // replace with returned values
                rgReaderStates[i].WindowsReaderState = readerstates[i];
            }

            return rc;
        }


        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardCancel(
            [In] IntPtr hContext);

        public SCardError Cancel(IntPtr hContext) {
            return SCardHelper.ToSCardError(SCardCancel(hContext));
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardGetAttrib(
            [In] IntPtr hCard,
            [In] Int32 dwAttrId,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbAttr,
            [In, Out] ref Int32 pcbAttrLen);

        public SCardError GetAttrib(IntPtr hCard, IntPtr dwAttrId, byte[] pbAttr, out int pcbAttrLen) {
            var attrlen = (pbAttr != null) 
                ? pbAttr.Length
                : 0;

            var rc = SCardHelper.ToSCardError(SCardGetAttrib(
                hCard,
                unchecked((Int32)dwAttrId.ToInt64()), // On a 64-bit platform IntPtr.ToInt32() will throw an OverflowException 
                pbAttr,
                ref attrlen));

            pcbAttrLen = attrlen;
            return rc;
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardSetAttrib(
            [In] IntPtr hCard,
            [In] Int32 dwAttrId,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] pbAttr,
            [In] Int32 cbAttrLen);

        public SCardError SetAttrib(IntPtr hCard, IntPtr dwAttrId, byte[] pbAttr, int attrSize) {
            // On a 64-bit platforms IntPtr.ToInt32() will throw an OverflowException 
            var attrid = unchecked((Int32) dwAttrId.ToInt64());
            var cbAttrLen = 0;
            
            if (pbAttr != null) {
                if (attrSize > pbAttr.Length || attrSize < 0) {
                    throw new ArgumentOutOfRangeException("attrSize");
                }
                cbAttrLen = attrSize;
            }

            return SCardHelper.ToSCardError(
                SCardSetAttrib(
                    hCard,
                    attrid,
                    pbAttr,
                    cbAttrLen));
        }

        [DllImport(WINSCARD_DLL, CharSet = CharSet.Auto)]
        private static extern Int32 SCardFreeMemory(
            [In] IntPtr hContext,
            [In] IntPtr pvMem);

        // Windows specific DLL imports

        [DllImport(KERNEL_DLL, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(String lpFileName);

        [DllImport(KERNEL_DLL, CharSet = CharSet.Ansi, ExactSpelling = true, EntryPoint = "GetProcAddress")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, String lpProcName);

        public IntPtr GetSymFromLib(string symName) {
            // Step 1. load dynamic link library
            if (_dllHandle == IntPtr.Zero) {
                _dllHandle = LoadLibrary(WINSCARD_DLL);
                if (_dllHandle.Equals(IntPtr.Zero)) {
                    throw new Exception("PInvoke call LoadLibrary() failed");
                }
            }
            // Step 2. search symbol name in memory
            var symPtr = GetProcAddress(_dllHandle, symName);

            if (symPtr.Equals(IntPtr.Zero)) {
                throw new Exception("PInvoke call GetProcAddress() failed");
            }

            return symPtr;
        }
    }
}
