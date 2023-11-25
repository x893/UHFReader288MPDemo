// UHFReader288MPVCDlg.cpp : ʵ���ļ�
//

#include "stdafx.h"
#include "UHFReader288MPVC.h"
#include "UHFReader288MPVCDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// ����Ӧ�ó��򡰹��ڡ��˵���� CAboutDlg �Ի���

class CAboutDlg : public CDialog
{
public:
	CAboutDlg();

// �Ի�������
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV ֧��

// ʵ��
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialog(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
END_MESSAGE_MAP()


// CUHFReader288MPVCDlg �Ի���




CUHFReader288MPVCDlg::CUHFReader288MPVCDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CUHFReader288MPVCDlg::IDD, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CUHFReader288MPVCDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_TAB1, m_tab);
}

BEGIN_MESSAGE_MAP(CUHFReader288MPVCDlg, CDialog)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	//}}AFX_MSG_MAP
	ON_NOTIFY(TCN_SELCHANGE, IDC_TAB1, &CUHFReader288MPVCDlg::OnTcnSelchangeTab1)
END_MESSAGE_MAP()


// CUHFReader288MPVCDlg ��Ϣ��������

BOOL CUHFReader288MPVCDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

	// ��������...���˵������ӵ�ϵͳ�˵��С�

	// IDM_ABOUTBOX ������ϵͳ���Χ�ڡ�
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		CString strAboutMenu;
		strAboutMenu.LoadString(IDS_ABOUTBOX);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// ���ô˶Ի����ͼ�ꡣ��Ӧ�ó��������ڲ��ǶԻ���ʱ����ܽ��Զ�
	//  ִ�д˲���
	SetIcon(m_hIcon, TRUE);			// ���ô�ͼ��
	SetIcon(m_hIcon, FALSE);		// ����Сͼ��
	m_tab.InsertItem(0,_T("Inventory"));
	m_tab.InsertItem(1,_T("Buffer Operation"));

	m_page1.Create(IDD_PAGE1,GetDlgItem(IDC_TAB1));
	m_page2.Create(IDD_PAGE2,GetDlgItem(IDC_TAB1));
	m_page1.ShowWindow(TRUE);
	m_page2.ShowWindow(FALSE);

	CRect rs;
	m_tab.GetClientRect(&rs);
	//�����ӶԻ����ڸ������е�λ��
	rs.top+=23; 
	rs.bottom-=2; 
	rs.left+=4;
	rs.right-=3; 
	//�����ӶԻ���ߴ粢�ƶ���ָ��λ��
	m_page1.MoveWindow(&rs);
	m_page2.MoveWindow(&rs);
	m_tab.SetCurSel(0); 
	// TODO: �ڴ����Ӷ���ĳ�ʼ������
	UINT arr[3];
	int i=0;
	for(i=0;i<3;i++)
	{
		arr[i]=200+i;
	}
	m_statusbar.Create(this);
	m_statusbar.SetIndicators(arr,sizeof(arr)/sizeof(UINT));
	for(i=0;i<3;i++)
	{
		m_statusbar.SetPaneInfo(i,arr[i],0,200);
	}
	m_statusbar.SetPaneInfo(2,arr[2],0,300);
	RepositionBars(AFX_IDW_CONTROLBAR_FIRST,AFX_IDW_CONTROLBAR_LAST,0);
	// TODO: �ڴ����Ӷ���ĳ�ʼ������

	return TRUE;  // ���ǽ��������õ��ؼ������򷵻� TRUE
}

void CUHFReader288MPVCDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialog::OnSysCommand(nID, lParam);
	}
}

// �����Ի���������С����ť������Ҫ����Ĵ���
//  �����Ƹ�ͼ�ꡣ����ʹ���ĵ�/��ͼģ�͵� MFC Ӧ�ó���
//  �⽫�ɿ���Զ���ɡ�

void CUHFReader288MPVCDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // ���ڻ��Ƶ��豸������

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// ʹͼ���ڹ����������о���
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// ����ͼ��
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialog::OnPaint();
	}
}

//���û��϶���С������ʱϵͳ���ô˺���ȡ�ù��
//��ʾ��
HCURSOR CUHFReader288MPVCDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}


void CUHFReader288MPVCDlg::OnTcnSelchangeTab1(NMHDR *pNMHDR, LRESULT *pResult)
{
	// TODO: �ڴ����ӿؼ�֪ͨ�����������
	int CurSel = m_tab.GetCurSel();
	switch(CurSel)
	{
	case 0:
		m_page1.ShowWindow(true);
		m_page2.ShowWindow(false);
		break;
	case 1:
		m_page1.ShowWindow(false);
		m_page2.ShowWindow(true);
		break;
	default:
		;
	}    
	*pResult = 0;
}