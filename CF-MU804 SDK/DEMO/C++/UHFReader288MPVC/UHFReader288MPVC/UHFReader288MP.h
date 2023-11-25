// 下列 ifdef 块是创建使从 DLL 导出更简单的
// 宏的标准方法。此 DLL 中的所有文件都是用命令行上定义的 UHFREADER288MP_EXPORTS
// 符号编译的。在使用此 DLL 的
// 任何其他项目上不应定义此符号。这样，源文件中包含此文件的任何其他项目都会将
// UHFREADER288MP_API 函数视为是从 DLL 导入的，而此 DLL 则将用此宏定义的
// 符号视为是被导出的。
#ifdef UHFREADER288MP_EXPORTS
#define UHFREADER288MP_API __declspec(dllexport)
#else
#define UHFREADER288MP_API __declspec(dllimport)
#endif
#include "Reader.h"
// 此类是从 UHFReader288MP.dll 导出的
class UHFREADER288MP_API CUHFReader288MP {
public:
	CUHFReader288MP(void);
	// TODO: 在此添加您的方法。
};

extern UHFREADER288MP_API int nUHFReader288MP;

UHFREADER288MP_API int fnUHFReader288MP(void);


#ifdef __cplusplus
extern "C" {
#endif
	UHFREADER288MP_API void InitRFIDCallBack(pRFIDCallBack pUIDback,BOOL isBackUID,int FrmHandle);
	UHFREADER288MP_API int CloseNetPort(int FrmHandle);
	UHFREADER288MP_API int OpenNetPort(int Port,
		LPCTSTR IPaddr,
		BYTE*ComAdr,
		int *Frmhandle);
	UHFREADER288MP_API int OpenUSBPort(BYTE *address,int* FrmHandle);
	UHFREADER288MP_API int CloseUSBPort(int FrmHandle);
	UHFREADER288MP_API int CloseComPort();

	UHFREADER288MP_API int CloseSpecComPort(int FrmHandle);

	UHFREADER288MP_API int GetReaderInformation(BYTE* ComAdr,				//读写器地址		
		BYTE* VersionInfo,			//软件版本
		BYTE* ReaderType,				//读写器型号
		BYTE* TrType,		//支持的协议
		BYTE* dmaxfre,           //当前读写器使用的最高频率
		BYTE* dminfre,           //当前读写器使用的最低频率
		BYTE* powerdBm,             //读写器的输出功率
		BYTE* ScanTime,
		BYTE*Ant,
		BYTE*BeepEn,
		BYTE*OutputRep,
		BYTE*CheckAnt,
		int FrmHandle);//最大询查时间

	UHFREADER288MP_API int OpenComPort(int port,
		BYTE *address,
		BYTE baud,
		int* FrmHandle);

	UHFREADER288MP_API int AutoOpenComPort(int *port,
		BYTE *address,
		BYTE baud,
		int *FrmHandle);

	UHFREADER288MP_API int Inventory_G2(BYTE *address,
		BYTE QValue,
		BYTE Session,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE MaskFlag,
		BYTE AdrTID,
		BYTE LenTID,
		BYTE TIDFlag,
		BYTE Target,
		BYTE InAnt,
		BYTE Scantime,
		BYTE FastFlag,
		BYTE *pEPCList,
		BYTE *Ant,
		int *Totallen,		
		int *CardNum,
		int FrmHandle);


	UHFREADER288MP_API int Fast_EPC_Inventory_G2(BYTE *address,
		BYTE QValue,
		BYTE Session,
		BYTE Target,
		BYTE ScanTime,
		BYTE *Ant,
		BYTE *pEPCList, 
		int *Totallen,		
		int *CardNum,
		int FrmHandle);
	UHFREADER288MP_API int InventoryMix_G2(BYTE *address,
		BYTE QValue,
		BYTE Session,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE MaskFlag,
		BYTE ReadMem,
		BYTE *ReadAdr,
		BYTE ReadLen,
		BYTE *Psd,
		BYTE Target,
		BYTE InAnt,
		BYTE Scantime,
		BYTE FastFlag,///////////////
		BYTE *pEPCList,
		BYTE *Ant,
		int *Totallen,		
		int *CardNum,
		int FrmHandle);


	UHFREADER288MP_API int Inventory_EPC_G2(BYTE *address,
		BYTE MatchType,
		BYTE *MatchLen,
		BYTE *MatchOffset,
		BYTE *EPCData,///////////////
		BYTE *pEPCList,
		BYTE *Ant,
		int *Totallen,		
		int *CardNum,
		int FrmHandle)	;

	UHFREADER288MP_API int Inventory_QT_G2(BYTE *address,
		BYTE QValue,
		BYTE Session,
		BYTE Target,
		BYTE InAnt,///////////////
		BYTE Scantime,
		BYTE FastFlag,
		BYTE *pEPCList,
		BYTE *Ant,
		int *Totallen,		
		int *CardNum,
		int FrmHandle);

	UHFREADER288MP_API int ReadData_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE Mem,
		BYTE WordPtr,
		BYTE Num,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE *Data,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int WriteData_G2(BYTE *address,
		BYTE *EPC,
		BYTE WNum,
		BYTE ENum,
		BYTE Mem,
		BYTE WordPtr,
		BYTE *Wdt,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int WriteEPC_G2(BYTE *address,						       
		BYTE *Password,
		BYTE *WriteEPC,
		BYTE ENum,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int KillTag_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE *Killpwd,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int Lock_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE select,
		BYTE setprotect,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int BlockErase_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE Mem,
		BYTE WordPtr,
		BYTE Num,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int SetPrivacyByEPC_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int SetPrivacyWithoutEPC_G2(BYTE *address,
		BYTE *Password,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int ResetPrivacy_G2(BYTE *address,
		BYTE *Password,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int CheckPrivacy_G2(BYTE *address,
		BYTE *readpro,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int EASConfigure_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE *Password,
		BYTE EAS,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int EASAlarm_G2(BYTE *address,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int BlockLock_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE *Password,
		BYTE WrdPointer,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int SingleTagInventory_G2(BYTE *address,
		BYTE* EPC,
		int *EPCLength,		
		int *CardNum,
		int FrmHandle);

	UHFREADER288MP_API int BlockWrite_G2(BYTE *address,
		BYTE *EPC,
		BYTE WNum,
		BYTE ENum,
		BYTE Mem,
		BYTE WordPtr,
		BYTE *Wdt,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int InventorySingle_6B(BYTE *address,
		BYTE *Ant,
		BYTE* ID_6B,
		int FrmHandle);

	UHFREADER288MP_API int InventoryMultiple_6B(BYTE *address,
		BYTE Condition,
		BYTE StartAddress,
		BYTE mask,
		BYTE *ConditionContent,
		BYTE *Ant,
		BYTE* ID_6B,
		int *Cardnum,
		int FrmHandle);

	UHFREADER288MP_API int ReadData_6B(BYTE *address,
		BYTE* ID_6B,
		BYTE StartAddress,
		BYTE Num,
		BYTE *Data,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int WriteData_6B(BYTE *address,
		BYTE *ID_6B,
		BYTE StartAddress,
		BYTE *Writedata,
		BYTE Writedatalen,
		BYTE *writtenbyte,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int Lock_6B(BYTE *address,
		BYTE *ID_6B,
		BYTE StartAddress,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int CheckLock_6B(BYTE *address,
		BYTE *ID_6B,
		BYTE StartAddress,
		BYTE *ReLockState,
		int *Errorcode,
		int FrmHandle)	;

	UHFREADER288MP_API int SetRegion(BYTE *address,
		BYTE dmaxfre,
		BYTE dminfre,
		int FrmHandle);

	UHFREADER288MP_API int SetAddress(BYTE *address,
		BYTE ComAdrData,
		int FrmHandle);

	UHFREADER288MP_API int SetInventoryScanTime(BYTE *address,
		BYTE ScanTime,
		int FrmHandle);

	UHFREADER288MP_API int InSelfTestMode(BYTE *address,
		BOOL IsSelfTestMode,
		int FrmHandle);

	UHFREADER288MP_API int SetBaudRate(BYTE *address,
		BYTE baud,
		int FrmHandle);

	UHFREADER288MP_API int SetDAC(BYTE *address,
		BYTE DACdata,
		int FrmHandle);

	UHFREADER288MP_API int GetDAC(BYTE *address,
		BYTE *DACdata,
		int FrmHandle);

	UHFREADER288MP_API int CheckPowerParameter(BYTE *address,
		BYTE *code,
		int FrmHandle);

	UHFREADER288MP_API int SolidifyDACandPowerlist(BYTE *address,
		BYTE *dBm_list,
		BYTE *code,
		int FrmHandle);

	UHFREADER288MP_API int GetStartInformation(BYTE *address,
		BYTE *FreE,
		BYTE *addrE,
		BYTE *scnE,
		BYTE *xpwrE,
		BYTE *wgE,
		BYTE *xRreadModeE,
		BYTE *xModeParaE,
		BYTE *xbpsE,
		BYTE *xEasAccuE,
		int FrmHandle);

	UHFREADER288MP_API int SetRfPower(BYTE *address,
		BYTE PowerDbm,
		int FrmHandle);

	UHFREADER288MP_API int RfOutput(BYTE *address,
		BYTE OnOff,
		int FrmHandle);

	UHFREADER288MP_API int DeleteRfOutput(BYTE *address,
		BYTE *Reinfo,
		int FrmHandle);

	UHFREADER288MP_API int BuzzerAndLEDControl(BYTE *address,
		BYTE AvtiveTime,
		BYTE SilentTime,
		BYTE Times,
		int FrmHandle);

	UHFREADER288MP_API int SetWorkMode(BYTE *address,
		BYTE Read_mode,
		int FrmHandle);

	UHFREADER288MP_API int GetSystemParameter(BYTE *address,
		BYTE *Read_mode,
		BYTE *Accuracy,
		BYTE *RepCondition,
		BYTE *RepPauseTime,
		BYTE *ReadPauseTim,
		BYTE *TagProtocol,
		BYTE *MaskMem,
		BYTE *MaskAdr,
		BYTE *MaskLen,
		BYTE *MaskData,
		BYTE *TriggerTime,
		BYTE *AdrTID,
		BYTE *LenTID,
		int FrmHandle);

	UHFREADER288MP_API int SetEASSensitivity(BYTE *ComAdr,
		BYTE Accuracy,
		int FrmHandle);

	UHFREADER288MP_API int SetTriggerTime(BYTE *ComAdr,
		BYTE TriggerTime,
		int FrmHandle);

	UHFREADER288MP_API int SetMask(BYTE *ComAdr,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int FrmHandle);

	UHFREADER288MP_API int SetResponsePamametersofAuto_runningMode(BYTE *ComAdr,
		BYTE RepCondition,
		BYTE RepPauseTime,
		int FrmHandle);

	UHFREADER288MP_API int SetInventoryInterval(BYTE *ComAdr,
		BYTE ReadPauseTim,
		int FrmHandle);

	UHFREADER288MP_API int SelectTagType(BYTE *ComAdr,
		BYTE TagProtocol,
		int FrmHandle);

	UHFREADER288MP_API int SetAntennaMultiplexing(BYTE *ComAdr,
		BYTE Ant,
		int FrmHandle);

	UHFREADER288MP_API int SetBeepNotification(BYTE *ComAdr,
		BYTE BeepEn,
		int FrmHandle);

	UHFREADER288MP_API int SetReal_timeClock(BYTE *ComAdr,
		BYTE *paramer,
		int FrmHandle);

	UHFREADER288MP_API int GetTime(BYTE *ComAdr,
		BYTE *paramer,
		int FrmHandle);

	UHFREADER288MP_API int GetTagBufferInfo(BYTE *ComAdr,
		BYTE *Data,
		int *dataLength,
		int FrmHandle);

	UHFREADER288MP_API int ClearTagBuffer(BYTE *ComAdr,
		int FrmHandle);

	UHFREADER288MP_API int SetRelay(BYTE *ComAdr,
		BYTE RelayTime,
		int FrmHandle);

	UHFREADER288MP_API int SetRevDelay(BYTE *ComAdr,
		BYTE RevDelay,
		int FrmHandle);

	UHFREADER288MP_API int GetRevDelay(BYTE *ComAdr,
		BYTE *RevDelay,
		int FrmHandle);

	UHFREADER288MP_API int GetGPIOStatus(BYTE *ComAdr,
		BYTE *OutputPin,
		int FrmHandle);

	UHFREADER288MP_API int SetGPIO(BYTE *ComAdr,
		BYTE OutputPin,
		int FrmHandle);

	UHFREADER288MP_API int SetNotificationPulseOutput(BYTE *ComAdr,
		BYTE OutputRep,
		int FrmHandle);

	UHFREADER288MP_API int ReadActiveModeData(BYTE * ScanModeData,
		int *ValidDatalength,
		int FrmHandle);

	UHFREADER288MP_API int SetTIDParameter(BYTE *address,
		BYTE AdrTID,
		BYTE LenTID,
		int FrmHandle);

	UHFREADER288MP_API int ChangeATMode(BYTE *address,
		BYTE ATMode,
		int FrmHandle);

	UHFREADER288MP_API int TransparentCMD(BYTE *ComAdr,
		BYTE timeout,
		BYTE nlen,
		BYTE *cmddata,
		BYTE *recvLen,
		BYTE *recvdata,
		int FrmHandle);

	UHFREADER288MP_API int SetSeriaNo(BYTE *address,
		BYTE* SeriaNo,
		int FrmHandle);

	UHFREADER288MP_API int GetSeriaNo(BYTE *address,
		BYTE* SeriaNo,
		int FrmHandle);

	UHFREADER288MP_API int EnableTestFreq(BYTE *address,
		BYTE* TestFreq,
		BYTE GainIndex,
		int FrmHandle);

	UHFREADER288MP_API int GetADValue(BYTE *address,
		BYTE nFlag,
		BYTE* ADValue,
		int FrmHandle);

	UHFREADER288MP_API int SetAValue(BYTE *address,
		BYTE nFlag,
		BYTE* A1,
		BYTE* A0,
		int FrmHandle);

	UHFREADER288MP_API int AutoTable(BYTE *address,
		int FrmHandle);

	UHFREADER288MP_API int AutoOffset(BYTE *address,
		int FrmHandle);

	UHFREADER288MP_API int GetAdjustPara(BYTE *address,
		BYTE *data,
		int FrmHandle);

	UHFREADER288MP_API int SetCheckAnt(BYTE *address,
		BYTE CheckAnt,
		int FrmHandle);

	UHFREADER288MP_API int GetCheckPWR(BYTE *address,
		BYTE *FWDPWR,
		BYTE *REVPWR,
		int FrmHandle);


	UHFREADER288MP_API int SetTagCustomFunction(BYTE *address,
		BYTE *InlayType,
		int FrmHandle);

	UHFREADER288MP_API int GetMonza4QTWorkParamter_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE *QTcontrol,
		int *Errorcode,
		int FrmHandle)	;

	UHFREADER288MP_API int SetMonza4QTWorkParamter_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE QTcontrol,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);


	UHFREADER288MP_API int SetCommType(BYTE *address,
		BYTE CommType,
		int FrmHandle);

	UHFREADER288MP_API int SetDCOffset(BYTE *address,
		BYTE* DCOffset,
		int FrmHandle);

	UHFREADER288MP_API int SetQS(BYTE *address,
		BYTE Qvalue,
		BYTE Session,
		int FrmHandle);

	UHFREADER288MP_API int GetQS(BYTE *address,
		BYTE *Qvalue,
		BYTE *Session,
		int FrmHandle);

	UHFREADER288MP_API int GetModuleVersion(BYTE *address,
		BYTE *Version,
		int FrmHandle);
	UHFREADER288MP_API int SetFlashRom(BYTE *address,
		int FrmHandle);

	UHFREADER288MP_API int ExtReadData_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE Mem,
		BYTE *WordPtr,
		BYTE Num,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE *Data,
		int *Errorcode,
		int FrmHandle)	;

	UHFREADER288MP_API int ExtWriteData_G2(BYTE *address,
		BYTE *EPC,
		BYTE WNum,
		BYTE ENum,
		BYTE Mem,
		BYTE *WordPtr,
		BYTE *Wdt,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int InventoryBuffer_G2(BYTE *address,
		BYTE QValue,
		BYTE Session,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE MaskFlag,
		BYTE AdrTID,
		BYTE LenTID,
		BYTE TIDFlag,///////////
		BYTE Target,
		BYTE InAnt,
		BYTE Scantime,
		BYTE FastFlag,///////////////
		int *BufferCount,		
		int *TagNum,
		int FrmHandle);

	UHFREADER288MP_API int SetSaveLen(BYTE *ComAdr,
		BYTE SaveLen,
		int FrmHandle);
	UHFREADER288MP_API int GetSaveLen(BYTE *ComAdr,
		BYTE *SaveLen,
		int FrmHandle);
	UHFREADER288MP_API int ReadBuffer_G2(BYTE *ComAdr,
		int *Totallen,
		int *CardNum,
		BYTE *pEPCList,
		int FrmHandle);
	UHFREADER288MP_API int ClearBuffer_G2(BYTE *ComAdr,
		int FrmHandle);
	UHFREADER288MP_API int GetBufferCnt_G2(BYTE *ComAdr,
		int *Count,
		int FrmHandle);
	UHFREADER288MP_API int SetReadMode(BYTE *ComAdr,
		BYTE ReadMode	,
		int FrmHandle);
	UHFREADER288MP_API int SetReadParameter(BYTE *ComAdr,
		BYTE *Parameter,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE MaskFlag,
		BYTE AdrTID,
		BYTE LenTID,
		BYTE TIDFlag,///////////
		int FrmHandle);
	UHFREADER288MP_API int GetReadParameter(BYTE *ComAdr,
		BYTE *Parameter,
		int FrmHandle);
	UHFREADER288MP_API int WriteRfPower(BYTE *address,
		BYTE PowerDbm,
		int FrmHandle);

	UHFREADER288MP_API int ReadRfPower(BYTE *address,
		BYTE *PowerDbm,
		int FrmHandle);

	UHFREADER288MP_API int RetryTimes(BYTE *address,
		BYTE *Times,
		int FrmHandle);



	UHFREADER288MP_API int GetTMP_G2(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE WordPtr,
		BYTE WaitTime,
		BYTE *Data,
		int *Errorcode,
		int FrmHandle);

	UHFREADER288MP_API int ConfigAntNum(BYTE *address,
		BYTE *AntNum,
		int FrmHandle);

	UHFREADER288MP_API int SetUserPwd(BYTE *address,
		BYTE *UserPwd,
		int FrmHandle);

	UHFREADER288MP_API int GetUserPwd(BYTE *address,
		BYTE *UserPwd,
		int FrmHandle);//
	UHFREADER288MP_API int SetUTCTime(BYTE *address,
		BYTE *UTCTime,
		int FrmHandle);
	UHFREADER288MP_API int GetEMTmpData(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE SendUID,
		BYTE NewSample,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE *UID,
		BYTE *SenserData,
		BYTE *UTC,
		int *Errorcode,
		int FrmHandle);
	UHFREADER288MP_API int GetSPI(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE SPICmdSize,
		BYTE SPIResSize,
		BYTE SPISclk,
		BYTE SPIInitDelay,
		BYTE SPIInterval,
		BYTE* SPICmd,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		BYTE *SPIResData,
		int *Errorcode,
		int FrmHandle);
	UHFREADER288MP_API int ResetAlarm(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE *Password,
		BYTE MaskMem,
		BYTE *MaskAdr,
		BYTE MaskLen,
		BYTE *MaskData,
		int *Errorcode,
		int FrmHandle);
	UHFREADER288MP_API int SetCarrierTime(BYTE *address,
		BYTE *CarrierTime,
		int FrmHandle);
	UHFREADER288MP_API int SecondIdentify(BYTE *address,
		BYTE *Token1,
		BYTE *Token2,
		BYTE *Handle,
		int FrmHandle);
	UHFREADER288MP_API int FirstIdentify(BYTE *address,
		BYTE *RT,
		int FrmHandle);
	UHFREADER288MP_API int QueryAndIdentify(BYTE *address,
		BYTE *EPC,
		BYTE ENum,
		BYTE *EPCLen,
		BYTE *NewEPC,
		BYTE *RSSI,
		BYTE *KI,
		BYTE *TID,
		int FrmHandle);
	UHFREADER288MP_API int SetHeartBeatTime(BYTE *address,
		BYTE *HeartBeatTime,
		int FrmHandle);
	UHFREADER288MP_API int SetFTime(BYTE *address,
		BYTE FTime,
		int FrmHandle);
	UHFREADER288MP_API int GetFTime(BYTE *address,
		BYTE* FTime,
		int FrmHandle);
	UHFREADER288MP_API int SetDRM(BYTE *address,
		BYTE DRM,
		int FrmHandle);
	UHFREADER288MP_API int GetDRM(BYTE *address,
		BYTE* DRM,
		int FrmHandle);

	UHFREADER288MP_API int SetAntennaA12(BYTE *ComAdr,
		BYTE SetOnce,
		BYTE AntCfg1,
		BYTE AntCfg2,
		int FrmHandle);

	UHFREADER288MP_API int GetReaderTemperature(BYTE *address,
		BYTE* PlusMinus,
		BYTE* Temperature,
		int FrmHandle);

	UHFREADER288MP_API int MeasureReturnLoss(BYTE *address,
		BYTE* TestFreq,
		BYTE Ant,
		BYTE*ReturnLoss,
		int FrmHandle);

#ifdef __cplusplus
}
#endif


