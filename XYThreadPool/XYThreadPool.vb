Option Explicit On 
Option Strict On

Imports System.Threading
Imports System.Collections

Public Delegate Sub ThreadErrorHandlerDelegate(ByVal oWorkItem As ThreadPoolWorkItem, ByVal oError As Exception)

Public Class ThreadPoolWorkItem
    Public m_bStoreOutput As Boolean = False
    Public m_sName As String = ""
    Public m_pMethod As [Delegate] = Nothing
    Public m_pInput As Object() = Nothing
    Public m_oOutput As Object = Nothing
    Public m_oException As Exception = Nothing
    Public Sub New()
    End Sub
    Public Sub New(ByVal sName As String, ByVal pMethod As [Delegate], ByVal pInput As Object(), ByVal bStoreOutput As Boolean)
        m_sName = sName
        m_pMethod = pMethod
        m_pInput = pInput
        m_bStoreOutput = bStoreOutput
    End Sub
End Class

Public Class XYThreadPool
    Private m_htThreads As Hashtable = New Hashtable(256)
    Private m_nMinThreadCount As Integer = 5
    Private m_nMaxThreadCount As Integer = 10
    Private m_nShutdownPause As Integer = 200
    Private m_nServerPause As Integer = 25
    Private m_bContinue As Boolean = False
    Private m_oException As Exception = Nothing
    Private m_qInput As Queue = New Queue(1024)
    Private m_qOutput As Queue = New Queue(1024)
    Private m_delegateThreadErrorHandler As [Delegate] = New ThreadErrorHandlerDelegate(AddressOf OnThreadError)
    Private Sub ThreadProc()
        While m_bContinue
            Dim obj As Object = Nothing
            Monitor.Enter(Me)
            If m_qInput.Count > 0 Then obj = m_qInput.Dequeue()
            Monitor.Exit(Me)
            If obj Is Nothing Then
                Dim bQuit As Boolean = False
                Monitor.Enter(Me)
                If m_htThreads.Count > m_nMinThreadCount Then
                    m_htThreads.Remove(Thread.CurrentThread.Name)
                    bQuit = True
                End If
                Monitor.Exit(Me)
                If bQuit Then Return
                Thread.Sleep(10 * m_nServerPause)
            Else
                Dim oWorkItem As ThreadPoolWorkItem = CType(obj, ThreadPoolWorkItem)
                Try
                    oWorkItem.m_oOutput = oWorkItem.m_pMethod.DynamicInvoke(oWorkItem.m_pInput)
                Catch oBug As Exception
                    If Not m_delegateThreadErrorHandler Is Nothing Then
                        Try
                            Dim pInput As Object() = {oWorkItem, oBug}
                            m_delegateThreadErrorHandler.DynamicInvoke(pInput)
                        Catch
                        End Try
                    End If
                End Try
                If oWorkItem.m_bStoreOutput Then
                    Monitor.Enter(m_qOutput)
                    m_qOutput.Enqueue(oWorkItem)
                    Monitor.Exit(m_qOutput)
                End If
                Thread.Sleep(m_nServerPause)
            End If
        End While
    End Sub
    Private Sub OnThreadError(ByVal oWorkItem As ThreadPoolWorkItem, ByVal oError As Exception)
        If oWorkItem Is Nothing Then
            m_oException = oError
        Else
            oWorkItem.m_oException = oError
        End If
    End Sub
    Public Sub SetThreadErrorHandler(ByVal pMethod As ThreadErrorHandlerDelegate)
        Monitor.Enter(Me)
        m_delegateThreadErrorHandler = pMethod
        Monitor.Exit(Me)
    End Sub
    Public Sub SetServerPause(ByVal nMilliseconds As Integer)
        Monitor.Enter(Me)
        If nMilliseconds > 9 And nMilliseconds < 101 Then m_nServerPause = nMilliseconds
        Monitor.Exit(Me)
    End Sub
    Public Sub SetShutdownPause(ByVal nMilliseconds As Integer)
        Monitor.Enter(Me)
        m_nShutdownPause = nMilliseconds
        Monitor.Exit(Me)
    End Sub
    Public Function GetException() As Exception
        Return m_oException
    End Function
    Public Sub InsertWorkItem(ByVal oWorkItem As ThreadPoolWorkItem)
        Try
            Monitor.Enter(Me)
            m_qInput.Enqueue(oWorkItem)
            If m_bContinue AndAlso m_qInput.Count > m_htThreads.Count AndAlso m_htThreads.Count < m_nMaxThreadCount Then
                Dim th As Thread = New Thread(AddressOf ThreadProc)
                ' th.Name = New Guid().NewGuid.ToString()
                th.Name = Guid.NewGuid.ToString()
                m_htThreads.Add(th.Name, th)
                th.Start()
            End If
        Catch oBug As Exception
            m_oException = oBug
        Finally
            Monitor.Exit(Me)
        End Try
    End Sub
    Public Sub InsertWorkItem(ByVal sName As String, ByVal pMethod As [Delegate], ByVal pArgs As Object(), ByVal bStoreOutput As Boolean)
        InsertWorkItem(New ThreadPoolWorkItem(sName, pMethod, pArgs, bStoreOutput))
    End Sub
    Public Function ExtractWorkItem() As ThreadPoolWorkItem
        Dim oWorkItem As Object = Nothing
        Monitor.Enter(m_qOutput)
        If m_qOutput.Count > 0 Then oWorkItem = m_qOutput.Dequeue()
        Monitor.Exit(m_qOutput)
        If oWorkItem Is Nothing Then Return Nothing
        Return CType(oWorkItem, ThreadPoolWorkItem)
    End Function
    Public Function StartThreadPool(Optional ByVal nMinThreadCount As Integer = 5, Optional ByVal nMaxThreadCount As Integer = 10) As Boolean
        Try
            Monitor.Enter(Me)
            If m_bContinue = False Then
                m_bContinue = True
                If nMinThreadCount > 0 Then
                    m_nMinThreadCount = nMinThreadCount
                End If
                If nMaxThreadCount > m_nMinThreadCount Then
                    m_nMaxThreadCount = nMaxThreadCount
                Else
                    m_nMaxThreadCount = 2 * m_nMinThreadCount
                End If
                Dim i As Integer
                For i = 1 To m_nMinThreadCount
                    Dim th As Thread = New Thread(AddressOf ThreadProc)
                    ' th.Name = New Guid().NewGuid.ToString()
                    th.Name = Guid.NewGuid.ToString()
                    m_htThreads.Add(th.Name, th)
                    th.Start()
                Next i
            End If
            Return True
        Catch oBug As Exception
            m_bContinue = False
            m_oException = oBug
            Return False
        Finally
            Monitor.Exit(Me)
        End Try
    End Function
    Public Sub StopThreadPool()
        Monitor.Enter(Me)
        m_bContinue = False
        Thread.Sleep(Math.Max(200, m_nShutdownPause))
        If (m_nShutdownPause > 0) Then
            Dim dict As IDictionaryEnumerator = m_htThreads.GetEnumerator()
            While dict.MoveNext()
                Dim th As Thread = CType(dict.Value(), Thread)
                If th.IsAlive Then
                    Try
                        th.Abort()
                    Catch
                    End Try
                End If
            End While
        End If
        m_htThreads.Clear()
        m_qInput.Clear()
        ' m_qOutput.Clear()
        Monitor.Exit(Me)
    End Sub
    Public Function GetThreadCount() As Integer
        Monitor.Enter(Me)
        Dim nCount As Integer = m_htThreads.Count
        Monitor.Exit(Me)
        Return nCount
    End Function
End Class
