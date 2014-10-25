Imports GR_NET
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Configuration
Imports System.Text.RegularExpressions
Imports System.Diagnostics
Module Module1
    Dim d As New AgapeConnectDataContext
    'Const MinAvg12 As Integer = 10
    'Const MinAvg3 As Integer = 11
    'Const MinAvg1 As Integer = 12
    'Const MinLess50 As Integer = 13
    'Const Min50to80 As Integer = 24
    'Const Min80to100 As Integer = 25
    'Const MinMore100 As Integer = 19
    'Const MinBudgetSpent As Integer = 20
    'Const MinLocal As Integer = 21
    'Const MinForeign As Integer = 22
    'Const MinSubsidy As Integer = 23

    'Public Const StaffBal As Integer = 4
    'Public Const StaffExp As Integer = 5
    'Public Const StaffIncFor As Integer = 6
    'Public Const StaffIncSub As Integer = 7
    'Public Const StaffIncLoc As Integer = 8


    Const GR_ACC_BAL_STAGE = "a3def60c-0cd8-11e4-b82c-12c37bb2d521"
    Const GR_ACC_EXP_STAGE = "ecd3c172-0d86-11e4-935d-12c37bb2d521"
    Const GR_ACC_INC_FOREIGN_STAGE = "29651f0a-0d87-11e4-8206-12c37bb2d521"
    Const GR_ACC_INC_LOC_STAGE = "10790362-0d87-11e4-aa87-12c37bb2d521"
    Const GR_ACC_INC_SUB_STAGE = "46868876-0d87-11e4-8935-12c37bb2d521"

    Const GR_ACC_BAL = "0b6c89fc-0d9b-11e4-9728-12543788cf06"
    Const GR_ACC_EXP = "28e506a8-0d9b-11e4-8947-12543788cf06"
    Const GR_ACC_INC_FOREIGN = "39f2fbda-0d9b-11e4-a72d-12543788cf06"
    Const GR_ACC_INC_LOC = "4575d5d6-0d9b-11e4-a61f-12543788cf06"
    Const GR_ACC_INC_SUB = "600e3e38-0d9b-11e4-b8c5-12543788cf06"

    Const GR_MPD_EXP = "88077c4c-0d9b-11e4-997d-12543788cf06"
    Const GR_MPD_GOAL = "b63003d2-0d9b-11e4-bcd4-12543788cf06"


    Const ExpenseBudget As Integer = 9
    Const ToRaiseBudget As Integer = 26
    Dim sinceDate As New Date
    Dim m As Dictionary(Of String, MeasurementType)
    Dim tnt As tntFetch
    Dim refresh As Boolean = True
    Dim gr_server As String = ""
    Dim access_token As String = ""
    Dim root_key = ""
    Dim stage As Boolean = False
    Dim excludeList As String() = {"id", "client_integration_id", "ministry", "authentication.key_guid", "authentication.client_integration_id"}
    Dim areas As New Dictionary(Of String, String)
    Dim staff_types As New Dictionary(Of String, String)

    Dim out_log As String = ""
    Dim areas_ents As List(Of Entity)

    Sub Main(ByVal args As String())


        If args.Contains("-help") Or args.Contains("-?") Then
            Console.WriteLine("-stage : Use GR-Stage")
            Console.WriteLine("-refresh : push all data from dnn (regardless of age)")
            Console.WriteLine("-nosync : Don't sync dnn")
            Console.WriteLine("-nompd : Don't sync MPD Dashbaord Data")
            Console.WriteLine("-noother : Don't sync Google (GMA/Stages) or tnt or systems dashboard")
            Return

        End If

        Log("starting sync at" & Now.ToString("yyyy-MM-dd hh:mm:ss"))

        If (args.Contains("-stage")) Then
            Log("using stage")
            gr_server = ConfigurationManager.AppSettings("gr_stage_server")
            access_token = ConfigurationManager.AppSettings("gma_status_stage_key")
            stage = True
        Else
            Log("using prodution")
            gr_server = ConfigurationManager.AppSettings("gr_server")
            access_token = ConfigurationManager.AppSettings("gma_status_api_key")
            root_key = ConfigurationManager.AppSettings("oib_root")
        End If
        Dim gr_area As New GR(root_key, gr_server, False)
        areas.Clear()
        areas_ents = gr_area.GetEntities("area", "")
        For Each row In areas_ents
            areas.Add(row.ID, row.GetPropertyValue("area_code"))
        Next


        refresh = args.Contains("-refresh")

        If (Not args.Contains("-nosync")) Then
            Try
                Log("Syncing People")
                SyncPeople()
            Catch ex As Exception
                Log("Error Syncing People: " & ex.ToString)
            End Try
        End If



        If (Not args.Contains("-nompd")) Then
            Try
                Log("Syncing Ministries")
                SyncMinisitries()
            Catch ex As Exception
                Log("Error Syncing Ministries: " & ex.ToString)
            End Try
        End If

        If (Not args.Contains("-noother")) Then
            Try
                Log("Syncing GMA (Google)")
                GoogleDrive.SyncValues(stage)
            Catch ex As Exception
                Log("Error Syncing GMA (Google): " & ex.ToString)
            End Try
            Try
                Log("Syncing StAGES")
                GoogleDrive.SyncStages(stage)
            Catch ex As Exception
                Log("Error Syncing Stages: " & ex.ToString)
            End Try
            Try
                Log("Syncing TnTUrls")
                tntFetch.SyncDnnUrls(stage)
            Catch ex As Exception
                Log("Error Syncing tnt urls: " & ex.ToString)
            End Try
            Try
                Log("Syncing System Status")
                SystemsStatus.GetSystemsStatus(stage)
            Catch ex As Exception

                Log("Error Syncing Systems Status: " & ex.ToString)
            End Try
            Try
                GoogleDrive.SyncPartnerMinistryNumber(stage)
            Catch ex As Exception
                Log("Error Syncing Partner Ministry Total: " & ex.ToString)
            End Try
        End If
        Log("finished at " & Now.ToString("yyyy-MM-dd hh:mm:ss"))
        WriteLog()
        Console.ReadKey()
        Return




    End Sub

    Public Sub Log(ByVal Message As String)
        out_log &= Message & vbNewLine
        Console.WriteLine(Message)
    End Sub
    Private Sub WriteLog()
        File.WriteAllText("OutputLog.txt", out_log)
    End Sub


    Private Sub SyncPeople(Optional ByVal PortalId As Integer = Nothing, Optional ByVal StaffId As Integer = Nothing)
        Dim apikeys = From c In d.AP_StaffBroker_Settings Where c.SettingName = "gr_api_key" And c.SettingValue <> "" And c.PortalId <> 2  'JUST PORTALID 2 FOR NOW
        If PortalId Then
            apikeys = apikeys.Where(Function(c) c.PortalId = PortalId)
        End If
        For Each api_key In apikeys
            Console.WriteLine("Processing portal:" & api_key.PortalId)
            'm = New Dictionary(Of String, MeasurementType)
            'InitMeasures()
            'For each of these portals get the staff
            'Dim dataserverURL = (From c In d.AP_StaffBroker_Settings Where c.PortalId = api_key.PortalId And c.SettingName = "DataserverURL" Select c.SettingValue).FirstOrDefault
            'If Not String.IsNullOrEmpty(dataserverURL) Then
            '    tnt = New tntFetch(dataserverURL)
            'Else
            '    tnt = Nothing
            'End If

            'Created GlobalRegistry_id profile property
            Dim ministry_id = (From c In d.AP_StaffBroker_Settings Where c.SettingName = "gr_ministry_id" And c.PortalId = api_key.PortalId Select c.SettingValue).FirstOrDefault





            If (Not String.IsNullOrEmpty(ministry_id)) Or api_key.PortalId = 5 Then


                CheckCreateProperty(api_key.PortalId, "gr_person_id", 349, 0)
                CheckCreateProperty(api_key.PortalId, "gr_ministry_membership_id", 349, 0)


                Dim gr As New GR(api_key.SettingValue, gr_server)
                If Not api_key.PortalId = 5 Then


                    Dim update As New Entity()
                    update.ID = ministry_id
                    update.AddPropertyValue("client_integration_id", api_key.PortalId)
                    Dim startPeriod = (From c In d.AP_StaffBroker_Settings Where c.SettingName = "FirstFiscalMonth" And c.PortalId = api_key.PortalId Select c.SettingValue).FirstOrDefault
                    If Not String.IsNullOrEmpty(startPeriod) Then
                        update.AddPropertyValue("fiscal_start_month", startPeriod)
                    End If
                    gr.UpdateEntity(update, "ministry")
                End If


                Dim st_whitelist = {"National Staff", "National Staff, Overseas", "Centrally Funded", "Overseas Staff, in Country", "Overseas Staff, Overseas"}
                Dim staff = From c In d.AP_StaffBroker_Staffs Where c.PortalId = api_key.PortalId And st_whitelist.Contains(c.AP_StaffBroker_StaffType.Name) And c.Active
                If StaffId Then
                    staff = staff.Where(Function(c) c.StaffId = StaffId)
                End If


                For Each s In staff
                    If Not s.User Is Nothing Then
                        Try



                            Console.WriteLine(s.DisplayName)
                            Dim p1 = syncPerson(s, s.User, api_key.PortalId, gr, ministry_id)



                            If Not s.User1 Is Nothing Then
                                Dim p2 = syncPerson(s, s.User1, api_key.PortalId, gr, ministry_id)
                                If (String.IsNullOrEmpty(GetProfileProperty(s.User1.UserProfiles, "gr_person_id", Nothing))) Then
                                    gr.RelateEntity("person", p1, "person", p2, "wife", "", s.UserId1, s.UserId2)
                                End If




                            End If
                        Catch ex As Exception
                            Log("error syncing " & s.DisplayName & " (" & s.StaffId & ")")
                        End Try
                    End If
                Next

                If Not api_key.PortalId = 5 Then


                    'Now sync leadership meta (now that all the people exist
                    For Each row In d.AP_StaffBroker_LeaderMetas.Where(Function(c) c.User.AP_StaffBroker_Staffs.PortalId = api_key.PortalId)
                        Dim gr_assignee_id = GetProfileProperty(row.User.UserProfiles, "gr_person_id", Nothing)

                        If Not gr_assignee_id Is Nothing Then
                            If Not row.Leader Is Nothing Then
                                Try


                                    Dim gr_leader_id = GetProfileProperty(row.Leader.UserProfiles, "gr_person_id", Nothing)
                                    If Not gr_leader_id Is Nothing Then
                                        gr.RelateEntity("person", gr_assignee_id, "person", gr_leader_id, "leader", "", row.UserId, row.LeaderId)
                                    End If
                                Catch ex As Exception
                                    Console.Write(ex.ToString)
                                End Try
                            End If

                            If Not row.Delegate Is Nothing Then
                                Dim gr_delegate_id = GetProfileProperty(row.User.UserProfiles, "gr_person_id", Nothing)
                                gr.RelateEntity("person", gr_assignee_id, "person", gr_delegate_id, "leader", "", row.UserId, row.LeaderId)

                            End If
                        End If





                        'TODO remove expired leadership assignments
                    Next
                End If



                '   PushMeasures(gr)
            End If

        Next

    End Sub
    'Private Sub PushMeasures(ByRef gr As GR)
    '    For Each item In m
    '        gr.AddUpdateMeasurement(item.Value)
    '    Next
    'End Sub
    'Private Sub InitMeasures()
    '    AddMeasureType("StaffBal", StaffBal)
    '    AddMeasureType("StaffExp", StaffExp)
    '    AddMeasureType("StaffIncFor", StaffIncFor)
    '    AddMeasureType("StaffIncSub", StaffIncSub)
    '    AddMeasureType("StaffIncLoc", StaffIncLoc)
    '    AddMeasureType("ExpenseBudget", ExpenseBudget)
    '    AddMeasureType("ToRaiseBudget", ToRaiseBudget)
    'End Sub
    'Private Sub AddMeasureType(ByVal name As String, ByVal id As Integer)
    '    Dim mt As New MeasurementType()
    '    mt.ID = id
    '    mt.Name = name
    '    m.Add(name, mt)
    'End Sub
    Private Sub SyncMinisitries()



        Dim gr_min As New GR(root_key, gr_server, False)
        Dim gr_back As New GR(root_key, "http://backend.global-registry.org/", False)



        'Get measureents


        'For Each the_country In d.AP_mpd_Countries



        '    Dim admins = gr_back.GetEntities("person", "&filters[owned_by]=all&filters[ministry:relationship][id]=" & the_country.gr_ministry_id & "&filters[ministry:relationship:role]=Administrator")
        '    admins.AddRange(gr_back.GetEntities("person", "&filters[owned_by]=all&filters[ministry:relationship][id]=" & the_country.gr_ministry_id & "&filters[ministry:relationship:role]=Finance Team"))
        '    admins.AddRange(gr_back.GetEntities("person", "&filters[owned_by]=all&filters[ministry:relationship][id]=" & the_country.gr_ministry_id & "&filters[ministry:relationship:role]=MPD Admin"))
        '    For Each admin In admins
        '        Dim ssoGuid = admin.GetPropertyValue("authentication.key_guid")
        '        If Not String.IsNullOrEmpty(ssoGuid) Then
        '            Dim q = From c In the_country.AP_MPD_CountryAdmins Where c.sso_guid = ssoGuid And c.ministry_id = the_country.mpdCountryId
        '            If q.Count = 0 Then
        '                Dim insert As New AP_MPD_CountryAdmin
        '                insert.ministry_id = the_country.mpdCountryId
        '                insert.sso_guid = ssoGuid
        '                d.AP_MPD_CountryAdmins.InsertOnSubmit(insert)
        '                d.SubmitChanges()
        '            End If
        '        End If

        '    Next
        '    Dim toDelete = (From c In d.AP_MPD_CountryAdmins Where admins.Where(Function(b) b.GetPropertyValue("authentication.key_guid") = c.sso_guid).Count = 0)
        '    For Each row In d.AP_MPD_CountryAdmins.Where(Function(c) c.ministry_id = the_country.mpdCountryId)
        '        If (From c In admins Where c.GetPropertyValue("authentication.key_guid") = row.sso_guid).Count = 0 Then
        '            d.AP_MPD_CountryAdmins.DeleteOnSubmit(row)
        '        End If


        '    Next
        '    d.SubmitChanges()


        'Next





        'Get Summary finance Data


        If refresh Then
            Log("refreshing user details")
            For Each person In d.Ap_mpd_Users
                Console.Write(".")
                Dim min_mem = gr_min.GetEntity(person.gr_min_membership_id)
                Dim p = gr_min.GetEntity(min_mem.GetPropertyValue("person.id"))


                person.Name = p.GetPropertyValue("last_name") & ", " & p.GetPropertyValue("first_name")
                person.Email = p.GetPropertyValue("email_address.email")
                person.Key_GUID = p.GetPropertyValue("authentication.key_guid")
                person.membership_type = min_mem.GetPropertyValue("membership_type")
                person.isNationalStaff = person.membership_type.Contains("National Staff")
                If p.collections.ContainsKey("leader:relationship") Then


                    For Each rel In p.collections("leader:relationship")
                        Dim q = From c In d.ap_mpd_user_reportings Where c.mpd_user_id = person.gr_person_id And c.mpd_leader_id = rel.GetPropertyValue("person")
                        If q.Count = 0 Then
                            Dim insert As New ap_mpd_user_reporting
                            insert.mpd_user_id = person.gr_person_id
                            insert.mpd_leader_id = rel.GetPropertyValue("person")
                            Dim leader = From c In d.Ap_mpd_Users Where c.gr_person_id = insert.mpd_leader_id And Not c.Key_GUID Is Nothing
                            If leader.Count > 0 Then
                                insert.leader_sso_guid = leader.First.Key_GUID
                            End If
                            d.ap_mpd_user_reportings.InsertOnSubmit(insert)
                            d.SubmitChanges()
                        End If
                    Next
                End If
            Next




            d.SubmitChanges()
        End If

        For Each row In areas_ents
            If row.collections.ContainsKey("admin:relationship") Then
                For Each admin In row.collections("admin:relationship")
                    Dim p = gr_min.GetEntity(admin.GetPropertyValue("person"))
                    Dim ssoCode = p.GetPropertyValue("authentication.key_guid")
                    If Not String.IsNullOrEmpty(ssoCode) Then
                        Dim q = From c In d.AP_mpd_AreaAdmins Where c.sso_guid = ssoCode And c.area = areas(row.ID)
                        If q.Count = 0 Then
                            Dim insert As New AP_mpd_AreaAdmin
                            insert.area = areas(row.ID)

                            insert.sso_guid = ssoCode

                            d.AP_mpd_AreaAdmins.InsertOnSubmit(insert)
                            d.SubmitChanges()


                        End If
                    End If

                Next
            End If
        Next

        For i As Integer = -12 To 0
            Dim allMeasurements As New List(Of Measurement)
            Log("PROCESSING " & Today.AddMonths(i).ToString("yyyy-MM"))
            Dim acc_bal = gr_min.GetMeasurements("", Today.AddMonths(i).ToString("yyyy-MM"), Today.AddMonths(i).ToString("yyyy-MM"), GR_ACC_BAL)  ' Need to get the rest
            Dim acc_exp = gr_min.GetMeasurements("", Today.AddMonths(i).ToString("yyyy-MM"), Today.AddMonths(i).ToString("yyyy-MM"), GR_ACC_EXP)  ' Need to get the rest
            Dim acc_inc_for = gr_min.GetMeasurements("", Today.AddMonths(i).ToString("yyyy-MM"), Today.AddMonths(i).ToString("yyyy-MM"), GR_ACC_INC_FOREIGN)  ' Need to get the rest
            Dim acc_inc_loc = gr_min.GetMeasurements("", Today.AddMonths(i).ToString("yyyy-MM"), Today.AddMonths(i).ToString("yyyy-MM"), GR_ACC_INC_LOC)  ' Need to get the rest
            Dim acc_inc_sub = gr_min.GetMeasurements("", Today.AddMonths(i).ToString("yyyy-MM"), Today.AddMonths(i).ToString("yyyy-MM"), GR_ACC_INC_SUB)  ' Need to get the rest
            Dim mpd_exp = gr_min.GetMeasurements("", Today.AddMonths(i).ToString("yyyy-MM"), Today.AddMonths(i).ToString("yyyy-MM"), GR_MPD_EXP)  ' Need to get the rest
            Dim mpd_goal = gr_min.GetMeasurements("", Today.AddMonths(i).ToString("yyyy-MM"), Today.AddMonths(i).ToString("yyyy-MM"), GR_MPD_GOAL)  ' Need to get the rest

            allMeasurements.AddRange(acc_bal.First.measurements)
            allMeasurements.AddRange(acc_exp.First.measurements)
            allMeasurements.AddRange(acc_inc_for.First.measurements)
            allMeasurements.AddRange(acc_inc_loc.First.measurements)
            allMeasurements.AddRange(acc_inc_sub.First.measurements)
            allMeasurements.AddRange(mpd_exp.First.measurements)
            allMeasurements.AddRange(mpd_goal.First.measurements)


            'combine and group by RID
            'for each RID


            For Each person In (From c In allMeasurements Group By c.RelatedEntityId Into Group)
                Console.Write(".")
                ''''Lookup person in database. 
                Dim dbUser = (From c In d.Ap_mpd_Users Where c.gr_min_membership_id = person.RelatedEntityId).FirstOrDefault

                If dbUser Is Nothing Then ' Add the Person
                    Dim min_mem = gr_min.GetEntity(person.RelatedEntityId)
                    Dim min_id = min_mem.GetPropertyValue("ministry.id")

                    Dim country = (From c In d.AP_mpd_Countries Where c.gr_ministry_id = min_id).FirstOrDefault
                    If country Is Nothing Then 'add the Country
                        Dim min_ent = gr_min.GetEntity(min_id)

                        country = New AP_mpd_Country
                        country.name = min_ent.GetPropertyValue("name")
                        country.gr_ministry_id = min_id
                        country.Area = areas(min_ent.GetPropertyValue("area:relationship.area"))
                        Dim iso = gr_min.GetEntity(min_ent.GetPropertyValue("iso_country:relationship.iso_country"))

                        country.isoCode = iso.GetPropertyValue("iso2_code")
                        d.AP_mpd_Countries.InsertOnSubmit(country)
                        d.SubmitChanges()

                    End If
                    Dim p = gr_min.GetEntity(min_mem.GetPropertyValue("person.id"))
                    dbUser = New Ap_mpd_User
                    dbUser.gr_min_membership_id = person.RelatedEntityId
                    dbUser.mpdCountryId = country.mpdCountryId
                    dbUser.Name = p.GetPropertyValue("last_name") & ", " & p.GetPropertyValue("first_name")
                    dbUser.Email = p.GetPropertyValue("email_address.email")
                    dbUser.Key_GUID = p.GetPropertyValue("authentication.key_guid")
                    dbUser.gr_person_id = min_mem.GetPropertyValue("person.id")

                    'lookup dnnUser
                    Try
                        dbUser.StaffId = (From c In d.UserProfiles Where c.PropertyValue = person.RelatedEntityId And c.ProfilePropertyDefinition.PropertyName = "gr_ministry_membership_id" Select c.User.AP_StaffBroker_Staffs.StaffId).FirstOrDefault

                    Catch ex As Exception

                    End Try

                    d.Ap_mpd_Users.InsertOnSubmit(dbUser)
                    d.SubmitChanges()
                    'Else

                    '    Dim min_mem = gr_min.GetEntity(person.RelatedEntityId)
                    '    Dim p = gr_min.GetEntity(min_mem.GetPropertyValue("person.id"))
                    '    dbUser.Key_GUID = p.GetPropertyValue("authentication.key_guid")
                    '    d.SubmitChanges()


                End If
                'update measurements
                Dim q = From c In d.AP_mpd_UserAccountInfos Where c.mpdUserId = dbUser.AP_mpd_UserId And c.period = Today.AddMonths(i).ToString("yyyy-MM")
                If dbUser.isNationalStaff Then



                    If q.Count > 0 Then
                        q.First.balance = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_BAL).Select(Function(c) c.Value).FirstOrDefault
                        q.First.expense = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_EXP).Select(Function(c) c.Value).FirstOrDefault
                        q.First.foreignIncome = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_FOREIGN).Select(Function(c) c.Value).FirstOrDefault
                        q.First.income = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_LOC).Select(Function(c) c.Value).FirstOrDefault
                        q.First.compensation = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_SUB).Select(Function(c) c.Value).FirstOrDefault


                        q.First.expBudget = person.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_EXP).Select(Function(c) c.Value).FirstOrDefault
                        q.First.toRaiseBudget = person.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_GOAL).Select(Function(c) c.Value).FirstOrDefault



                    Else
                        Dim insertM As New AP_mpd_UserAccountInfo
                        insertM.balance = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_BAL).Select(Function(c) c.Value).FirstOrDefault
                        insertM.expense = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_EXP).Select(Function(c) c.Value).FirstOrDefault
                        insertM.foreignIncome = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_FOREIGN).Select(Function(c) c.Value).FirstOrDefault
                        insertM.income = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_LOC).Select(Function(c) c.Value).FirstOrDefault
                        insertM.compensation = person.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_SUB).Select(Function(c) c.Value).FirstOrDefault
                        insertM.expBudget = person.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_EXP).Select(Function(c) c.Value).FirstOrDefault
                        insertM.toRaiseBudget = person.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_GOAL).Select(Function(c) c.Value).FirstOrDefault
                        insertM.period = Today.AddMonths(i).ToString("yyyy-MM")
                        insertM.mpdUserId = dbUser.AP_mpd_UserId
                        insertM.mpdCountryId = dbUser.mpdCountryId
                        d.AP_mpd_UserAccountInfos.InsertOnSubmit(insertM)

                    End If
                Else

                    d.AP_mpd_UserAccountInfos.DeleteAllOnSubmit(q)
                End If







            Next
            d.SubmitChanges()
        Next

        Log("Calculating person summaries")
        For Each dbUser In d.Ap_mpd_Users.Where(Function(c) c.isNationalStaff)
            Console.Write(".")
            Dim userAvg = (From c In d.AP_mpd_UserAccountInfos Where c.mpdUserId = dbUser.AP_mpd_UserId And Not (c.expense = 0 And c.income = 0) Order By c.period Descending).Take(13)

            If userAvg.Count > 1 Then
                'ignore the last!       
                userAvg = userAvg.OrderBy(Function(c) c.period).Take(userAvg.Count - 1)
            End If

            If userAvg.Count > 0 Then
                dbUser.AvgExpenses = userAvg.Average(Function(c) c.expense)

                dbUser.AvgExpenseBudget12 = userAvg.Average(Function(c) c.expBudget)
                dbUser.AvgMPDBudget12 = userAvg.Average(Function(c) c.toRaiseBudget)



                dbUser.AvgIncome12 = (From c In userAvg Select (c.income + c.foreignIncome)).Average
                dbUser.AvgIncome3 = (From c In userAvg.Take(3) Select (c.income + c.foreignIncome)).Average
                dbUser.AvgIncome1 = (From c In userAvg.Take(1) Select (c.income + c.foreignIncome)).Average
                If dbUser.AvgMPDBudget12 > 0 Then
                    Dim AvSup = userAvg.Where(Function(c) c.toRaiseBudget > 0 And c.income + c.foreignIncome > 0).ToList

                    Try


                        dbUser.AvgSupLevel12 = (From c In AvSup Select (c.income + c.foreignIncome) / c.toRaiseBudget).Average()
                        dbUser.AvgSupLevel3 = (From c In AvSup Select (c.income + c.foreignIncome) / c.toRaiseBudget).Average
                        dbUser.AvgSupLevel1 = (From c In AvSup Where c.income + c.foreignIncome > 0).Select(Function(c) (c.income + c.foreignIncome) / c.toRaiseBudget).FirstOrDefault
                    Catch ex As Exception
                        dbUser.AvgSupLevel12 = 0
                        dbUser.AvgSupLevel3 = 0
                        dbUser.AvgSupLevel1 = 0
                    End Try
                    dbUser.EstSupLevel12 = (From c In userAvg Select (c.income + c.foreignIncome)).Average() / (dbUser.AvgExpenses * -1.1)
                    dbUser.EstSupLevel3 = (From c In userAvg.Take(3) Select (c.income + c.foreignIncome)).Average / (dbUser.AvgExpenses * -1.1)
                    dbUser.EstSupLevel1 = (From c In userAvg Where c.income + c.foreignIncome > 0).Select(Function(c) (c.income + c.foreignIncome)).FirstOrDefault / (dbUser.AvgExpenses * -1.1)
                ElseIf dbUser.AvgExpenses <> 0 Then
                    dbUser.AvgSupLevel12 = 0
                    dbUser.AvgSupLevel3 = 0
                    dbUser.AvgSupLevel1 = 0
                    dbUser.EstSupLevel12 = (From c In userAvg Select (c.income + c.foreignIncome)).Average() / (dbUser.AvgExpenses * -1.1)
                    dbUser.EstSupLevel3 = (From c In userAvg.Take(3) Select (c.income + c.foreignIncome)).Average / (dbUser.AvgExpenses * -1.1)
                    dbUser.EstSupLevel1 = (From c In userAvg Where c.income + c.foreignIncome > 0).Select(Function(c) (c.income + c.foreignIncome)).FirstOrDefault / (dbUser.AvgExpenses * -1.1)

                End If


                dbUser.ForeignIncome12 = userAvg.Average(Function(c) c.foreignIncome)
                dbUser.LocalIncome12 = userAvg.Average(Function(c) c.income)
                dbUser.SubsidyIncome12 = userAvg.Average(Function(c) c.compensation)

                dbUser.AvgIncome = dbUser.ForeignIncome12 + dbUser.LocalIncome12 + dbUser.SubsidyIncome12
                If dbUser.AvgIncome > 0 Then
                    dbUser.SplitLocal = userAvg.Average(Function(c) c.income) / dbUser.AvgIncome
                    dbUser.SplitForeign = userAvg.Average(Function(c) c.foreignIncome) / dbUser.AvgIncome
                    dbUser.SplitSubsidy = userAvg.Average(Function(c) c.compensation) / dbUser.AvgIncome
                Else
                    dbUser.SplitLocal = 1
                    dbUser.SplitForeign = 0
                    dbUser.SplitSubsidy = 0
                End If

            Else
                dbUser.AvgExpenses = 0
                dbUser.AvgExpenseBudget12 = 0
                dbUser.AvgMPDBudget12 = 0
                dbUser.SplitLocal = 1
                dbUser.SplitForeign = 0
                dbUser.SplitSubsidy = 0
                dbUser.AvgSupLevel12 = 0
                dbUser.AvgSupLevel3 = 0
                dbUser.AvgSupLevel1 = 0
                dbUser.AvgIncome12 = 0
                dbUser.AvgIncome3 = 0
                dbUser.AvgIncome1 = 0
                dbUser.ForeignIncome12 = 0
                dbUser.LocalIncome12 = 0
                dbUser.SubsidyIncome12 = 0

            End If
        Next

        d.SubmitChanges()





        Log("Calculating country summaries")
        For Each the_country In d.AP_mpd_Countries
            Console.Write(".")
            If the_country.portalId Is Nothing Then
                the_country.portalId = (From c In d.AP_StaffBroker_Settings Where c.SettingName = "gr_ministry_id" And c.SettingValue = the_country.gr_ministry_id Select c.PortalId).FirstOrDefault()
            End If

            If refresh Then
                Dim min_ent = gr_min.GetEntity(the_country.gr_ministry_id)
                the_country.Area = areas(min_ent.GetPropertyValue("area:relationship.area"))

            End If


            '  Dim countryAvg = From c In the_country.Ap_mpd_Users Where c.mpdCountryId = the_country.mpdCountryId
            Dim countryUsers = From c In the_country.Ap_mpd_Users Where c.isNationalStaff
            the_country.lastUpdated = Now

            the_country.VeryLowCount = (From c In countryUsers Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 < 0.5).Count
            the_country.LowCount = (From c In countryUsers Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 0.5 And c.AvgSupLevel12 < 0.8).Count
            the_country.HighCount = (From c In countryUsers Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 0.8 And c.AvgSupLevel12 < 1.0).Count
            the_country.FullCount = (From c In countryUsers Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 1.0).Count


            the_country.EstVeryLowCount = (From c In countryUsers Where c.EstSupLevel12 < 0.5).Count
            the_country.EstLowCount = (From c In countryUsers Where c.EstSupLevel12 >= 0.5 And c.EstSupLevel12 < 0.8).Count
            the_country.EstHighCount = (From c In countryUsers Where c.EstSupLevel12 >= 0.8 And c.EstSupLevel12 < 1.0).Count
            the_country.EstFullCount = (From c In countryUsers Where c.EstSupLevel12 >= 1.0).Count


            If (countryUsers.Count > 0) Then

                Dim total_avg_income = countryUsers.Sum(Function(c) c.AvgIncome)
                If total_avg_income > 0 Then


                    the_country.SplitLocal = countryUsers.Sum(Function(c) c.LocalIncome12) / total_avg_income
                    the_country.SplitForeign = countryUsers.Sum(Function(c) c.ForeignIncome12) / total_avg_income
                    the_country.SplitSubsidy = countryUsers.Sum(Function(c) c.SubsidyIncome12) / total_avg_income
                Else
                    the_country.SplitLocal = 1
                    the_country.SplitForeign = 0
                    the_country.SplitSubsidy = 0
                End If
                the_country.NoBudgetCount = countryUsers.Where(Function(c) c.AvgExpenseBudget12 = 0).Count
                If (countryUsers.Where(Function(c) c.AvgExpenseBudget12 > 0)).Count > 0 Then
                    the_country.BudgetAccuracy = countryUsers.Where(Function(c) c.AvgExpenseBudget12 > 0 And c.AvgExpenses <> 0).Average(Function(c) -c.AvgExpenses / c.AvgExpenseBudget12)
                Else

                    the_country.BudgetAccuracy = 1
                End If

                the_country.AvgSupport12 = countryUsers.Average(Function(c) c.AvgSupLevel12)
                the_country.AvgSupport3 = countryUsers.Average(Function(c) c.AvgSupLevel3)
                the_country.AvgSupport1 = countryUsers.Average(Function(c) c.AvgSupLevel1)

                the_country.EstAvgSupport12 = countryUsers.Average(Function(c) c.EstSupLevel12)
                the_country.EstAvgSupport3 = countryUsers.Average(Function(c) c.EstSupLevel3)
                the_country.EstAvgSupport1 = countryUsers.Average(Function(c) c.EstSupLevel1)


            Else
                the_country.SplitLocal = 1
                the_country.SplitForeign = 0
                the_country.SplitSubsidy = 0

                the_country.NoBudgetCount = 0

                the_country.BudgetAccuracy = 1
                the_country.AvgSupport12 = 0
                the_country.AvgSupport3 = 0
                the_country.AvgSupport1 = 0
                the_country.EstAvgSupport12 = 0
                the_country.EstAvgSupport3 = 0
                the_country.EstAvgSupport1 = 0
            End If
            d.SubmitChanges()



            Dim admins = gr_back.GetEntities("person", "&filters[owned_by]=all&filters[ministry:relationship][id]=" & the_country.gr_ministry_id & "&filters[ministry:relationship:role]=Administrator")
            admins.AddRange(gr_back.GetEntities("person", "&filters[owned_by]=all&filters[ministry:relationship][id]=" & the_country.gr_ministry_id & "&filters[ministry:relationship:role]=Finance Team"))
            admins.AddRange(gr_back.GetEntities("person", "&filters[owned_by]=all&filters[ministry:relationship][id]=" & the_country.gr_ministry_id & "&filters[ministry:relationship:role]=MPD Admin"))
            For Each admin In admins
                Dim ssoGuid = admin.GetPropertyValue("authentication.key_guid")
                If Not String.IsNullOrEmpty(ssoGuid) Then
                    Dim q = From c In the_country.AP_MPD_CountryAdmins Where c.sso_guid = ssoGuid And c.ministry_id = the_country.mpdCountryId
                    If q.Count = 0 Then
                        Dim insert As New AP_MPD_CountryAdmin
                        insert.ministry_id = the_country.mpdCountryId
                        insert.sso_guid = ssoGuid
                        d.AP_MPD_CountryAdmins.InsertOnSubmit(insert)
                        d.SubmitChanges()
                    End If
                End If

            Next
            Dim toDelete = (From c In d.AP_MPD_CountryAdmins Where admins.Where(Function(b) b.GetPropertyValue("authentication.key_guid") = c.sso_guid).Count = 0)
            For Each row In d.AP_MPD_CountryAdmins.Where(Function(c) c.ministry_id = the_country.mpdCountryId)
                If (From c In admins Where c.GetPropertyValue("authentication.key_guid") = row.sso_guid).Count = 0 Then
                    d.AP_MPD_CountryAdmins.DeleteOnSubmit(row)
                End If


            Next
            d.SubmitChanges()








        Next









        Return






    End Sub


    Private Function GetValueFromMeasurementType(ByVal measurement_type As Integer, ByVal period As String, ByRef measures As List(Of MeasurementType)) As Double
        Dim this_measure = From c In measures Where c.ID = measurement_type

        If this_measure.Count > 0 Then

            Dim answer = From c In this_measure.First.measurements Where c.Period = period Select c.Value

            If answer.Count > 0 Then
                Return answer.First
            Else
                Return 0
            End If
        End If



        Return 0

    End Function

    Public Sub WriteLog(ByVal str As String)
        ' Console.Write(Now.ToString("mm:ss") & "-" & str & vbNewLine)

    End Sub

    Function isValidEmail(ByVal emailString As String) As Boolean



        Dim emailRegEx As New Regex("(\S+)@([^\.\s]+)(?:\.([^\.\s]+))+")

        Return emailRegEx.IsMatch(emailString)

    End Function

    Private Sub SetupDicts()
        staff_types.Clear()
        staff_types.Add("National Staff", "National Staff")
        staff_types.Add("National Staff, Overseas", "National Staff Overseas")
        staff_types.Add("Overseas Staff, in Country", "Overseas Staff In Country")
        staff_types.Add("Overseas Staff, Overseas", "Overseas Staff Overseas")
        staff_types.Add("Centrally Funded", "Centrally Funded")
        staff_types.Add("Ex-Staff", "Ex-Staff")

    End Sub
    Private Function syncPerson(ByVal s As AP_StaffBroker_Staff, ByVal u As User, ByVal Portalid As Integer, ByRef gr As GR, ByVal ministry_id As String) As String
        Dim has_data As Boolean = False
        SetupDicts()
        'Log("Sync " & u.DisplayName)
        Dim p = gr.People.createPerson(u.UserID)
        Dim gr_id = GetProfileProperty(u.UserProfiles, "gr_person_id", Nothing)
        p.AddPropertyValue("client_integration_id", u.UserID)
        sinceDate = Today.AddDays(-3)
        If String.IsNullOrEmpty(gr_id) Then
            sinceDate = Nothing
            'create the entity 

            gr_id = gr.CreateEntity(p, "person")
            SetProfileProperty(u.UserID, Portalid, "gr_person_id", gr_id)



        End If
        Dim isAdmin = u.UserRoles.Where(Function(c) c.Role.RoleName = "Administrators").Count > 0
        Dim isFinance = u.UserRoles.Where(Function(c) c.Role.RoleName = "Accounts Team").Count > 0
        Dim isMPD = u.UserRoles.Where(Function(c) c.Role.RoleName = "MPD Admin").Count > 0
        Dim isArea = u.UserRoles.Where(Function(c) c.Role.RoleName = "AreaAdmin").Count > 0
        p.ID = gr_id
        If Not Portalid = 5 Then


            p.AddPropertyValue("ministry:relationship.ministry", ministry_id)
            p.AddPropertyValue("ministry:relationship.client_integration_id", u.UserID & "_" & Portalid)
            Dim i As Integer = 0
            If isAdmin Then
                p.AddPropertyValue("ministry:relationship.role[" & i & "]", "Administrator")
                i += 1
            End If
            If isFinance Then
                p.AddPropertyValue("ministry:relationship.role[" & i & "]", "Finance Team")
                i += 1
            End If
            If isMPD Then
                p.AddPropertyValue("ministry:relationship.role[" & i & "]", "MPD Admin")
                i += 1
            End If
        End If
        If isArea Then
            Dim area_code = (From c In d.AP_StaffBroker_Settings Where c.PortalId = Portalid And c.SettingName = "area_code" Select c.SettingValue).FirstOrDefault
            If Not String.IsNullOrEmpty(area_code) Then
                Dim area_id = areas.Where(Function(c) c.Value = area_code).Select(Function(c) c.Key).FirstOrDefault
                If Not String.IsNullOrEmpty(area_id) Then
                    p.AddPropertyValue("area:relationship.area", area_id)
                    p.AddPropertyValue("area:relationship.client_integration_id", u.UserID & "_" & area_code)

                End If

            End If
        End If
        If (staff_types.ContainsKey(s.AP_StaffBroker_StaffType.Name)) And Portalid <> 5 Then
            p.AddPropertyValue("ministry:relationship.membership_type", staff_types(s.AP_StaffBroker_StaffType.Name))

        End If

        Dim casGuid = GetProfileProperty(u.UserProfiles, "ssoGUID", Nothing)
        If String.IsNullOrEmpty(casGuid) Then

            'there is no cas guid... lets be nice and look it up now!
            casGuid = GetSsoGuid(u.Username.TrimEnd(Portalid.ToString))
            If Not String.IsNullOrEmpty(casGuid) Then
                SetProfileProperty(u.UserID, Portalid, "ssoGUID", casGuid)
            End If

        End If
        If Not String.IsNullOrEmpty(casGuid) Then
            p.AddPropertyValue("authentication.key_guid", casGuid)
            p.AddPropertyValue("authentication.client_integration_id", casGuid)
        End If

        If refresh Then
            sinceDate = Nothing


        End If

        'Dim person_mappings = d.gr_mappings.Where(Function(c) c.PortalId = Portalid And Not c.gr_dot_notated_name.Contains("person:{in_this_min}"))
        Dim person_mappings = d.gr_mappings.Where(Function(c) c.PortalId = Portalid)

        For Each link In person_mappings.Where(Function(c) c.LocalSource = "U")

            p.AddPropertyValue(link.gr_dot_notated_name.Replace("person.", "").Replace("person:{in_this_min}", "ministry:relationship"), GetUserProperty(link.LocalName, u, sinceDate))
            If (link.gr_dot_notated_name.Contains(".email_address.email")) Then
                p.AddPropertyValue("email_address.client_integration_id", GetUserProperty(link.LocalName, u, sinceDate))
            End If

        Next
        For Each link In person_mappings.Where(Function(c) c.LocalSource = "UP")
            p.AddPropertyValue(link.gr_dot_notated_name.Replace("person.", "").Replace("person:{in_this_min}", "ministry:relationship"), Transform(link.FieldType, link.replace, GetProfileProperty(u.UserProfiles, link.LocalName, sinceDate)))
            If (link.gr_dot_notated_name.Contains("authentication.key_guid")) Then
                p.AddPropertyValue("authentication.client_integration_id", GetProfileProperty(u.UserProfiles, link.LocalName, sinceDate))
            End If
        Next

        For Each link In person_mappings.Where(Function(c) c.LocalSource = "S")
            p.AddPropertyValue(link.gr_dot_notated_name.Replace("person.", "").Replace("person:{in_this_min}", "ministry:relationship"), GetStaffProperty(link.LocalName, s, sinceDate))
        Next
        For Each link In person_mappings.Where(Function(c) c.LocalSource = "SP")
            p.AddPropertyValue(link.gr_dot_notated_name.Replace("person.", "").Replace("person:{in_this_min}", "ministry:relationship"), Transform(link.FieldType, link.replace, GetStaffProfileProperty(s.AP_StaffBroker_StaffProfiles, link.LocalName, sinceDate)))
        Next
        'WriteLog("Push " & u.DisplayName)



        If p.profileProperties.Keys.Where(Function(c) Not excludeList.Contains(c)).Count = 0 Then
            If p.collections.Count = 1 Then
                If p.collections.Values.First.First.profileProperties.Keys.Where(Function(c) Not excludeList.Contains(c)).Count = 0 Then
                    Return gr_id
                End If
            End If
        End If



        gr_id = gr.UpdateEntity(p, "person")
        If Not Portalid = 5 Then
            Dim rel_id = GetProfileProperty(u.UserProfiles, "gr_ministry_membership_id", Nothing)

            If String.IsNullOrEmpty(rel_id) Then
                Dim the_person = gr.GetEntity(gr_id)
                SetProfileProperty(u.UserID, Portalid, "gr_ministry_membership_id", the_person.GetPropertyValue("ministry:relationship.relationship_entity_id"))

            End If
        End If



        Return gr_id


    End Function


    Private Function GetSsoGuid(ByVal Username As String) As String
        Try


            Dim web As New WebClient()
            web.Encoding = Encoding.UTF8

            Dim endpoint = "https://thekey.me/cas/api/" & ConfigurationManager.AppSettings("thekey_key") & "/user/attributes?email=" & Username
            Dim xml = web.DownloadString(endpoint)
            If xml.Contains("""guid"" value=""") Then
                Return xml.Substring(xml.IndexOf("""guid"" value=""") + 14, 36)

            End If
        Catch ex As Exception

        End Try
        Return ""
    End Function
    Public Function getBudgetForStaffPeriod(ByVal StaffId As Integer, ByVal Period As String) As AP_mpdCalc_StaffBudget
        'Dim d As New MPD.MPDDataContext

        Dim q = From c In d.AP_mpdCalc_StaffBudgets Where c.StaffId = StaffId And (c.Status = 3 Or c.Status = 2) Order By c.BudgetPeriodStart Descending
        Dim r = q.Where(Function(c) c.BudgetPeriodStart <= Period).OrderByDescending(Function(c) c.BudgetPeriodStart)
        If r.Count > 0 Then
            Return r.First
        End If
        If q.Count > 0 Then
            Return q.First
        End If
        q = From c In d.AP_mpdCalc_StaffBudgets Where c.StaffId = StaffId And (c.Status = 0) Order By c.BudgetPeriodStart Descending
        If q.Count > 0 Then
            Return q.First
        End If

        Return Nothing
    End Function
    Private Function GetUserProperty(ByVal prop As String, ByVal u As User, ByVal SinceDate As Date?) As String
        If Not SinceDate Is Nothing Then
            If u.LastModifiedOnDate < SinceDate Then
                Return Nothing
            End If
        End If

        Select Case prop
            Case "FirstName"
                Return u.FirstName
            Case "LastName"
                Return u.LastName
            Case "DisplayName"
                Return u.DisplayName

            Case "Email"
                Return u.Email
            Case "UserId"
                Return u.UserID
            Case Else
                Return Nothing
        End Select

    End Function

    Private Function GetProfileProperty(ByVal userProfile As Data.Linq.EntitySet(Of UserProfile), ByVal propertyName As String, ByVal SinceDate As Date?) As String

        Try

            Dim prop = userProfile.Where(Function(c) c.ProfilePropertyDefinition.PropertyName = propertyName)
            If Not SinceDate Is Nothing Then
                If prop.First.LastUpdatedDate < SinceDate Then
                    Return Nothing
                End If
            End If


            Return prop.First.PropertyValue

        Catch ex As Exception

        End Try

        Return Nothing
    End Function

    Private Function GetStaffProperty(ByVal prop As String, ByVal s As AP_StaffBroker_Staff, ByVal sinceDate As Date?) As String
        If Not sinceDate Is Nothing Then
            If s.last_updated < sinceDate Then
                Return Nothing
            End If
        End If


        Select Case prop
            Case "R/C"
                Return s.CostCenter
            Case "StaffId"
                Return s.StaffId
            Case "DisplayName"
                Return s.DisplayName


            Case Else
                Return Nothing
        End Select

    End Function

    Private Function Transform(ByVal FieldType As String, ByVal replace As String, ByVal Value As String) As String
        If FieldType = "boolean" Then
            Return (FieldType = "True").ToString.ToLower

        ElseIf FieldType = "uk_date" Then
            Try
                Dim dt = Date.Parse(Value, New System.Globalization.CultureInfo("en-GB"))
                Return dt.ToString("yyyy-MM-dd")

            Catch ex As Exception
                Return ""
            End Try
        ElseIf FieldType = "email" Then
            If isValidEmail(Value) Then
                Return Value
            Else
                Return ""
            End If
        ElseIf Not String.IsNullOrEmpty(replace) Then
            Try


                Dim jss = New System.Web.Script.Serialization.JavaScriptSerializer()
                Dim tuples = jss.Deserialize(Of Dictionary(Of String, String))(replace)
                For Each row In tuples
                    Value = Value.Replace(row.Key, row.Value)
                Next
            Catch ex As Exception

            End Try

        End If
        Return Value

    End Function
    Private Function GetStaffProfileProperty(ByVal StaffProfile As Data.Linq.EntitySet(Of AP_StaffBroker_StaffProfile), ByVal propertyName As String, ByVal sinceDate As Date?) As String

        Try
            Dim q = StaffProfile.Where(Function(c) c.AP_StaffBroker_StaffPropertyDefinition.PropertyName = propertyName)
            If Not sinceDate Is Nothing Then
                If q.First.last_updated < sinceDate Then
                    Return Nothing
                End If
            End If


            Return q.Select(Function(c) c.PropertyValue)
        Catch ex As Exception

        End Try

        Return ""
    End Function

    Private Sub CheckCreateProperty(ByVal PortalId As Integer, ByVal PropertyName As String, ByVal Datatype As Integer, ByVal Length As Integer)
        Dim q = From c In d.ProfilePropertyDefinitions Where c.PortalID = PortalId And c.PropertyName = PropertyName

        If q.Count = 0 Then
            Dim insert As New ProfilePropertyDefinition
            insert.CreatedByUserID = 1
            insert.CreatedOnDate = Now
            insert.DataType = Datatype
            insert.ModuleDefID = -1
            insert.Deleted = False
            insert.DefaultValue = -1
            insert.PropertyCategory = "GlobalRegistry"
            insert.PropertyName = PropertyName
            insert.PortalID = PortalId
            insert.Length = Length
            insert.Required = False
            insert.ValidationExpression = ""
            insert.ViewOrder = 201
            insert.Visible = False
            insert.LastModifiedByUserID = 1
            insert.LastModifiedOnDate = Now
            insert.DefaultVisibility = 2
            insert.ReadOnly = False
            d.ProfilePropertyDefinitions.InsertOnSubmit(insert)
            d.SubmitChanges()

        End If

    End Sub

    Private Function SetProfileProperty(ByVal userId As Integer, ByVal PortalId As Integer, ByVal PropertyName As String, ByVal PropertyValue As String)
        Dim q = From c In d.UserProfiles Where c.UserID = userId And c.ProfilePropertyDefinition.PropertyName = PropertyName And c.ProfilePropertyDefinition.PortalID = PortalId

        If q.Count > 0 Then
            q.First.PropertyValue = PropertyValue
            q.First.LastUpdatedDate = Now
        Else
            Dim insert As New UserProfile
            insert.UserID = userId
            insert.PropertyDefinitionID = d.ProfilePropertyDefinitions.Where(Function(c) c.PortalID = PortalId And c.PropertyName = PropertyName).Select(Function(c) c.PropertyDefinitionID).FirstOrDefault
            insert.PropertyValue = PropertyValue
            insert.Visibility = 2
            insert.LastUpdatedDate = Now
            d.UserProfiles.InsertOnSubmit(insert)
        End If
        d.SubmitChanges()
    End Function
End Module
