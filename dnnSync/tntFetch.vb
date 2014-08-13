Imports System.Net
Imports System.Text
Imports System.IO
Imports GR_NET
Imports System.Configuration
Public Class tntFetch
    Private this_RC As tnt.GetFinancialTransactionsResult
    Public t As tnt.TntMPDDataServerWebService2
    Private sessionId As String
    Private myInfo As tnt.WebUserInfo
    Private addedThisTime As New ArrayList()
    Public Sub WriteLog(ByVal str As String)
        '  Console.Write(Now.ToString("mm:ss") & "-" & str & vbNewLine)

    End Sub


    Public Shared Sub SyncDnnUrls(Optional ByVal Stage As Boolean = False)

           Dim gr As GR
        If Stage Then

            gr = New GR(ConfigurationManager.AppSettings("jon_test_stage_key"), ConfigurationManager.AppSettings("gr_stage_server"))
        Else
            gr = New GR(ConfigurationManager.AppSettings("jon_test_api_key"), ConfigurationManager.AppSettings("gr_server"))
        End If

       
        Dim ministry = gr.GetEntities("ministry", "&ruleset=global_ministries")

        'For Each row In ministry
        '    Console.WriteLine(row.GetPropertyValue("min_code") & ", " & row.GetPropertyValue("name"))
        'Next

        Dim tntmpdlist = New FileStream("TntMPD_Organizations.csv", FileMode.Open)
        If Not tntmpdlist Is Nothing Then
            Dim tntmpdliststream = New StreamReader(tntmpdlist)

            Dim tntmpdline = tntmpdliststream.ReadLine()
            tntmpdline = tntmpdliststream.ReadLine()
            While (Not tntmpdline Is Nothing)

                Dim tntmpdlineArray = tntmpdline.Split(",")
                If (tntmpdlineArray(1).ToString().Contains("tntdataserver.eu") Or tntmpdlineArray(1).ToString().Contains("tntdataserverasia.com") Or tntmpdlineArray(1).ToString().Contains("tntdataserver.com") Or tntmpdlineArray(1).ToString().Contains("dataserver.tntware.com")) Then

                    Dim code = tntmpdlineArray(3)
                    If Not String.IsNullOrEmpty(code) Then
                        Dim min = From c In ministry Where c.GetPropertyValue("min_code") = code

                        If min.Count > 0 Then
                            Try
                                Module1.Log(min.First.GetPropertyValue("name"))

                                Dim tnt_url = tntmpdlineArray(1).Substring(0, tntmpdlineArray(1).LastIndexOf("dataquery/"))
                                Dim t = New tnt.TntMPDDataServerWebService2
                                '  Dim tntURL = country.AP_StaffBroker_Settings.Where(Function(c) c.SettingName = "DataserverURL" And c.PortalId = country.portalId).First.SettingValue

                                t.Url = tnt_url & "dataquery/dataqueryservice2.asmx"

                                t.Discover()
                                Dim props = t.Core_GetSiteProperties()

                                Dim insert As New GR_NET.Entity()
                                insert.ID = min.First.ID
                                insert.AddPropertyValue("last_dataserver_transaction", IIf(props.MaxFinTranDate = "12:00:00 AM", "", props.MaxFinTranDate.ToString("yyyy-MM-dd")))

                                insert.AddPropertyValue("last_dataserver_donation", IIf(props.MaxGiftDate = "12:00:00 AM", "", props.MaxGiftDate.ToString("yyyy-MM-dd")))

                                insert.AddPropertyValue("dataserver_url", tnt_url)
                                If Not String.IsNullOrEmpty(props.OrgLogo) Then
                                    insert.AddPropertyValue("logo_url", props.OrgLogo)
                                End If

                                insert.AddPropertyValue("client_integration_id", code)
                                gr.UpdateEntity(insert, "ministry")


                            Catch ex As Exception
                                Module1.Log("Problem syncing " & min.First.GetPropertyValue("name"))
                            End Try


                        End If
                    End If



                End If


                tntmpdline = tntmpdliststream.ReadLine()
            End While

            tntmpdliststream.Close()


        End If
    End Sub


    Public Sub New(ByVal tntURL As String)



        Dim d As New AgapeConnectDataContext


        Dim countries = From c In d.AP_mpd_Countries
        Dim tuPass = (From c In d.AP_StaffBroker_Settings Where c.PortalId = 0 And c.SettingName = "TrustedUserPassword" Select c.SettingValue).First



        Dim service = "https://www.agapeconnect.me" ' "https://tntdataserver.com/dataserver/mkd/" ' country.AP_StaffBroker_Settings.Where(Function(c) c.SettingName = "DataserverURL" And c.PortalId = country.portalId).First.SettingValue
        Dim restServer As String = "https://thekey.me/cas/v1/tickets/"
        Dim postData = "service=" & service & "&username=trusteduser@agapeconnect.me&password=" & tuPass

        Dim request As WebRequest = WebRequest.Create(restServer)
        Dim byteArray As Byte() = Encoding.UTF8.GetBytes(postData)
        request.ContentLength = byteArray.Length
        request.Method = "POST"
        request.ContentType = "application/x-www-form-urlencoded"

        Dim datastream As Stream = request.GetRequestStream
        datastream.Write(byteArray, 0, byteArray.Length)
        datastream.Close()
        Dim response As WebResponse = request.GetResponse
        restServer = response.Headers.GetValues("Location").ToArray()(0)


        t = New tnt.TntMPDDataServerWebService2
        '  Dim tntURL = country.AP_StaffBroker_Settings.Where(Function(c) c.SettingName = "DataserverURL" And c.PortalId = country.portalId).First.SettingValue

        t.Url = tntURL & "dataquery/dataqueryservice2.asmx"

        t.Discover()




        postData = "service=" & tntURL
        request = WebRequest.Create(restServer)
        byteArray = Encoding.UTF8.GetBytes(postData)
        request.ContentLength = byteArray.Length
        request.Method = "POST"
        request.ContentType = "application/x-www-form-urlencoded"

        datastream = request.GetRequestStream
        datastream.Write(byteArray, 0, byteArray.Length)
        datastream.Close()

        response = request.GetResponse

        response.Headers.GetValues("location")
        datastream = response.GetResponseStream
        Dim reader As New StreamReader(datastream)
        Dim ST = reader.ReadToEnd()

        sessionId = t.Auth_Login(tntURL, ST, False).SessionID

        myInfo = t.MyWebUser_GetInfo(sessionId)


        '   Dim allInfo = t.WebUser_GetAllInfo(sessionId)


        ' Dim allInfo = t.WebUser_GetAllInfo(sessionId)
        If (myInfo.StaffProfiles.Where(Function(c) c.Code = "All Accounts").Count = 0) Then
            t.WebUserMgmt_AddStaffProfile(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", "All Accounts", "All Financial Accounts")
            t.WebUserMgmt_StaffProfile_AddFinancialAccount(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", "All Accounts", "", True, "")

        End If

      

    End Sub


    Public Sub pushSummariesForStaffmember(ByVal RC As String, ByRef gr As GR_NET.GR, ByVal gr_person_id As String, ByRef measures As Dictionary(Of String, GR_NET.MeasurementType))
        If (myInfo.StaffProfiles.Where(Function(c) c.Code = RC).Count = 0) And Not addedThisTime.Contains(RC) Then
            t.WebUserMgmt_AddStaffProfile(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", RC, "RC-" & RC)
            t.WebUserMgmt_StaffProfile_AddFinancialAccount(sessionId, "4EA08A9D-1D66-5BBD-71A4-6F4C59FAE37E", RC, RC, False, "")
            addedThisTime.Add(RC)
        End If
        Dim d As New AgapeConnectDataContext


        'For Each Staff



        Dim startDate = New Date(Today.AddMonths(-6).Year, Today.AddMonths(-14).Month, 1)
        Dim EndDate = New Date(Today.AddMonths(1).Year, Today.AddMonths(1).Month, 1).AddDays(-1)

        WriteLog("Getting transactions")
        this_RC = t.StaffPortal_GetFinancialTransactions(sessionId, RC, startDate, EndDate, "", False)


        WriteLog("Got transactions")



        Dim AccountInfo = this_RC.FinancialTransactions
        If this_RC.FinancialAccounts.Count > 0 Then


            Dim Trx = From c In AccountInfo
                Group By y = c.TransactionDate.Year, m = c.TransactionDate.Month Into Group
                Select y, m, Income = Group.Where(Function(x) x.GLAccountIsIncome).Sum(Function(x) x.Amount),
                Expense = Group.Where(Function(x) Not x.GLAccountIsIncome).Sum(Function(x) x.Amount),
                Foreign = Group.Where(Function(x) x.GLAccountIsIncome And x.GLAccountCode.StartsWith("50")).Sum(Function(x) x.Amount),
                Allocation = Group.Where(Function(x) x.GLAccountIsIncome And (x.GLAccountCode.StartsWith("57") Or x.GLAccountCode.StartsWith("56"))).Sum(Function(x) x.Amount),
                Turnover = Group.Sum(Function(x) x.Amount)
                Order By y, m
            Dim Bal = this_RC.FinancialAccounts.First.BeginningBalance
            '    Console.Write("StartBal: " & Bal & vbNewLine)


            For Each mon In Trx
                Dim period = New Date(mon.y, mon.m, 1).ToString("yyyy-MM")
                WriteLog("Pushing " & period)

                ' Console.Write(period & vbNewLine)
                Bal += mon.Turnover
                measures("StaffBal").addMeasurement(gr_person_id, period, Bal)
                measures("StaffIncFor").addMeasurement(gr_person_id, period, mon.Foreign)
                measures("StaffIncSub").addMeasurement(gr_person_id, period, mon.Allocation)
                measures("StaffIncLoc").addMeasurement(gr_person_id, period, mon.Income - mon.Allocation)
                measures("StaffExp").addMeasurement(gr_person_id, period, -mon.Expense)


                'gr.AddUpdateMeasurement(gr_person_id, StaffBal, period, Bal)
                'WriteLog("Bal " & period)
                'gr.AddUpdateMeasurement(gr_person_id, StaffIncFor, period, mon.Foreign)
                'WriteLog("For " & period)
                'gr.AddUpdateMeasurement(gr_person_id, StaffIncSub, period, mon.Allocation)
                'WriteLog("Sub " & period)
                'gr.AddUpdateMeasurement(gr_person_id, StaffIncLoc, period, mon.Income - mon.Allocation)
                'WriteLog("Loc " & period)
                'gr.AddUpdateMeasurement(gr_person_id, StaffExp, period, -mon.Expense)
                'WriteLog("Exp " & period)

            Next

        End If


    End Sub



End Class
