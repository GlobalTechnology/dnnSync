Imports GR_NET
Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Configuration
Imports System.Text.RegularExpressions
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


    Sub Main(ByVal args As String())


        If (args.Contains("-stage")) Then
            gr_server = ConfigurationManager.AppSettings("gr_stage_server")
            access_token = ConfigurationManager.AppSettings("gma_status_stage_key")
            stage = True
        Else
            gr_server = ConfigurationManager.AppSettings("gr_server")
            access_token = ConfigurationManager.AppSettings("gma_status_api_key")
            root_key = ConfigurationManager.AppSettings("oib_root")
        End If

      



        'Dim gr As New GR("cd27e0f4767db1330c599b1377e608048987deddedf39984f7a552609f17", gr_server)


        'Dim rel_ents = gr.GetEntities("ministry_membership", "", 0, 60, 0)
        'For Each ent In rel_ents
        '    gr.DeleteEntity(ent.ID)

        'Next
        'Return


        refresh = args.Contains("-refresh")

        'SyncPeople(19, 433)
        'Return
        If (Not args.Contains("-nosync")) Then
            SyncPeople()
        End If



        If (Not args.Contains("-nompd")) Then
            SyncMinisitries()
        End If


        'GoogleDrive.SyncValues(stage)
        'GoogleDrive.SyncStages(stage)
        'tntFetch.SyncDnnUrls(stage)
        'SystemsStatus.GetSystemsStatus(stage)
        'Return
        Console.Write("finished")
        Console.ReadKey()
        Return




    End Sub

  


    Private Sub SyncPeople(Optional ByVal PortalId As Integer = Nothing, Optional ByVal StaffId As Integer = nothing)
        Dim apikeys = From c In d.AP_StaffBroker_Settings Where c.SettingName = "gr_api_key" And c.SettingValue <> ""   'JUST PORTALID 2 FOR NOW
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





            If Not String.IsNullOrEmpty(ministry_id) Then


                CheckCreateProperty(api_key.PortalId, "gr_person_id", 349, 0)
                CheckCreateProperty(api_key.PortalId, "gr_ministry_membership_id", 349, 0)


                Dim gr As New GR(api_key.SettingValue, gr_server)

                Dim update As New Entity()
                update.ID = ministry_id
                update.AddPropertyValue("client_integration_id", api_key.PortalId)
                Dim startPeriod = (From c In d.AP_StaffBroker_Settings Where c.SettingName = "FirstFiscalMonth" And c.PortalId = api_key.PortalId Select c.SettingValue).FirstOrDefault
                If Not String.IsNullOrEmpty(startPeriod) Then
                    update.AddPropertyValue("fiscal_start_month", startPeriod)
                End If
                gr.UpdateEntity(update, "ministry")



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
                            Console.WriteLine("error syncing " & s.DisplayName & " (" & s.StaffId & ")")
                        End Try
                    End If
                Next



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
        WriteLog("Sync Ministries")
        Dim gr_min As New GR(root_key, gr_server)

        'Get measureents


        
        'Get Summary finance Data


        For i As Integer = -12 To 0
            Dim allMeasurements As New List(Of Measurement)
            Console.WriteLine("PROCESSING " & Today.AddMonths(i).ToString("yyyy-MM"))
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
                End If
                'update measurements
                For Each meas In (From c In person.Group Group By c.Period Into Group)
                    Dim q = From c In d.AP_mpd_UserAccountInfos Where c.mpdUserId = dbUser.AP_mpd_UserId And c.period = meas.Period
                    If q.Count > 0 Then
                        q.First.balance = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_BAL).Sum(Function(c) c.Value)
                        q.First.expense = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_EXP).Sum(Function(c) c.Value)
                        q.First.foreignIncome = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_FOREIGN).Sum(Function(c) c.Value)
                        q.First.income = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_LOC).Sum(Function(c) c.Value)
                        q.First.compensation = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_SUB).Sum(Function(c) c.Value)


                        q.First.expBudget = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_EXP).Sum(Function(c) c.Value)
                        q.First.toRaiseBudget = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_GOAL).Sum(Function(c) c.Value)
                  


                    Else
                    Dim insertM As New AP_mpd_UserAccountInfo
                    insertM.balance = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_BAL).Sum(Function(c) c.Value)
                    insertM.expense = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_EXP).Sum(Function(c) c.Value)
                    insertM.foreignIncome = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_FOREIGN).Sum(Function(c) c.Value)
                    insertM.income = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_LOC).Sum(Function(c) c.Value)
                    insertM.compensation = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_ACC_INC_SUB).Sum(Function(c) c.Value)
                    insertM.expBudget = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_EXP).Sum(Function(c) c.Value)
                    insertM.toRaiseBudget = meas.Group.Where(Function(c) c.MeasurementTypeId = GR_MPD_GOAL).Sum(Function(c) c.Value)
                    insertM.period = meas.Period
                    insertM.mpdUserId = dbUser.AP_mpd_UserId
                    insertM.mpdCountryId = dbUser.mpdCountryId
                    d.AP_mpd_UserAccountInfos.InsertOnSubmit(insertM)

                    End If

                Next
                d.SubmitChanges()

                If i = 0 Then
                    'if the last time - calculate averages


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
                            dbUser.AvgSupLevel12 = (From c In userAvg Where c.toRaiseBudget > 0 Select (c.income + c.foreignIncome) / c.toRaiseBudget).Average()
                            dbUser.AvgSupLevel3 = (From c In userAvg.Take(3) Where c.toRaiseBudget > 0 Select (c.income + c.foreignIncome) / c.toRaiseBudget).Average
                            dbUser.AvgSupLevel1 = (From c In userAvg Where c.toRaiseBudget > 0 And c.income + c.foreignIncome > 0).Select(Function(c) (c.income + c.foreignIncome) / c.toRaiseBudget).FirstOrDefault
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

                End If





            Next
        Next
        For Each the_country In d.AP_mpd_Countries

            If the_country.portalId Is Nothing Then
                the_country.portalId = (From c In d.AP_StaffBroker_Settings Where c.SettingName = "gr_ministry_id" And c.SettingValue = the_country.gr_ministry_id Select c.PortalId).FirstOrDefault()
            End If

            Dim countryAvg = From c In the_country.Ap_mpd_Users Where c.mpdCountryId = the_country.mpdCountryId

            the_country.lastUpdated = Now

            the_country.VeryLowCount = (From c In the_country.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 < 0.5).Count
            the_country.LowCount = (From c In the_country.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 0.5 And c.AvgSupLevel12 < 0.8).Count
            the_country.HighCount = (From c In the_country.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 0.8 And c.AvgSupLevel12 < 1.0).Count
            the_country.FullCount = (From c In the_country.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 1.0).Count


            the_country.EstVeryLowCount = (From c In the_country.Ap_mpd_Users Where c.EstSupLevel12 < 0.5).Count
            the_country.EstLowCount = (From c In the_country.Ap_mpd_Users Where c.EstSupLevel12 >= 0.5 And c.EstSupLevel12 < 0.8).Count
            the_country.EstHighCount = (From c In the_country.Ap_mpd_Users Where c.EstSupLevel12 >= 0.8 And c.EstSupLevel12 < 1.0).Count
            the_country.EstFullCount = (From c In the_country.Ap_mpd_Users Where c.EstSupLevel12 >= 1.0).Count


            If (countryAvg.Count > 0) Then

                Dim total_avg_income = countryAvg.Sum(Function(c) c.AvgIncome)
                the_country.SplitLocal = countryAvg.Sum(Function(c) c.LocalIncome12) / total_avg_income
                the_country.SplitForeign = countryAvg.Sum(Function(c) c.ForeignIncome12) / total_avg_income
                the_country.SplitSubsidy = countryAvg.Sum(Function(c) c.SubsidyIncome12) / total_avg_income

                the_country.NoBudgetCount = countryAvg.Count(Function(c) c.AvgExpenseBudget12 = 0)
                If (countryAvg.Where(Function(c) c.AvgExpenseBudget12 > 0)).Count > 0 Then
                    the_country.BudgetAccuracy = countryAvg.Where(Function(c) c.AvgExpenseBudget12 > 0).Average(Function(c) c.AvgExpenses / c.AvgExpenseBudget12)

                End If

                the_country.AvgSupport12 = countryAvg.Average(Function(c) c.AvgSupLevel12)
                the_country.AvgSupport3 = countryAvg.Average(Function(c) c.AvgSupLevel3)
                the_country.AvgSupport1 = countryAvg.Average(Function(c) c.AvgSupLevel1)

                the_country.EstAvgSupport12 = countryAvg.Average(Function(c) c.EstSupLevel12)
                the_country.EstAvgSupport3 = countryAvg.Average(Function(c) c.EstSupLevel3)
                the_country.EstAvgSupport1 = countryAvg.Average(Function(c) c.EstSupLevel1)


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
        Next

        d.SubmitChanges()


        Return

        ''Sync Ministries

        'Dim ministries = gr_min.GetEntities("ministry", "&ruleset=global_ministries", Nothing, Nothing)

        'For Each min In ministries 'Iterate through all ministries
        '    'Find the Country
        '    Dim the_country As AP_mpd_Country
        '    Dim q = From c In d.AP_mpd_Countries Where c.gr_ministry_id = min.ID
        '    WriteLog("Syncing " & min.GetPropertyValue("name"))
        '    If q.Count > 0 Then
        '        the_country = q.First
        '        the_country.name = min.GetPropertyValue("name")
        '        the_country.isoCode = min.GetPropertyValue("country_iso2")

        '    Else
        '        the_country = New AP_mpd_Country
        '        the_country.name = min.GetPropertyValue("name")
        '        the_country.isoCode = min.GetPropertyValue("country_iso2")
        '        the_country.gr_ministry_id = min.ID
        '        d.AP_mpd_Countries.InsertOnSubmit(the_country)
        '        d.SubmitChanges()

        '    End If

        '    Dim sys_min = From c In d.ministry_systems Where c.gr_ministry_id = min.ID

        '    If sys_min.Count > 0 Then
        '        sys_min.First.gr_ministry_id = min.ID
        '        sys_min.First.min_code = min.GetPropertyValue("min_code")
        '        sys_min.First.iso2_code = the_country.isoCode
        '        sys_min.First.min_name = the_country.name
        '        sys_min.First.min_logo = min.GetPropertyValue("logo_url")
        '        If Not min.GetPropertyValue("last_dataserver_donation") = "" Then
        '            sys_min.First.last_dataserver_donation = min.GetPropertyValue("last_dataserver_donation")
        '        End If
        '        If Not min.GetPropertyValue("last_financial_report") = "" Then
        '            sys_min.First.last_fin_rep = min.GetPropertyValue("last_financial_report")
        '        End If
        '        If Not min.GetPropertyValue("last_ministry_status") = "" Then
        '            sys_min.First.gma_status = min.GetPropertyValue("last_ministry_status")
        '        End If
        '    Else
        '        Dim insert As New ministry_system
        '        insert.iso2_code = the_country.isoCode
        '        insert.min_code = min.GetPropertyValue("min_code")
        '        insert.min_name = the_country.name
        '        insert.min_logo = min.GetPropertyValue("logo_url")
        '        If Not min.GetPropertyValue("last_dataserver_donation") = "" Then
        '            insert.last_dataserver_donation = min.GetPropertyValue("last_dataserver_donation")
        '        End If
        '        If Not min.GetPropertyValue("last_financial_report") = "" Then
        '            insert.last_fin_rep = min.GetPropertyValue("last_financial_report")
        '        End If
        '        If Not min.GetPropertyValue("last_ministry_status") = "" Then
        '            insert.gma_status = min.GetPropertyValue("last_ministry_status")
        '        End If

        '        d.ministry_systems.InsertOnSubmit(insert)
        '    End If
        '    d.SubmitChanges()










        '    If False Then



        '        'get Staff
        '        Dim person = gr_min.GetEntities("person", "&filters[ministry:relationship]=" & min.ID & "&filters[owned_by]=all")
        '        For Each p In person
        '            'check if we already have a record
        '            Dim thisPerson As New Ap_mpd_User
        '            Dim checkP = From c In the_country.Ap_mpd_Users Where c.gr_person_id = p.ID
        '            If checkP.Count > 0 Then
        '                thisPerson = checkP.First
        '                thisPerson.Name = p.GetPropertyValue("last_name.value") & ", " & p.GetPropertyValue("first_name.value")
        '                thisPerson.Email = p.GetPropertyValue("email.value")
        '                thisPerson.Key_GUID = p.GetPropertyValue("authentication.key_guid.value")

        '            Else
        '                thisPerson = New Ap_mpd_User
        '                thisPerson.Name = p.GetPropertyValue("last_name.value") & ", " & p.GetPropertyValue("first_name.value")
        '                thisPerson.Email = p.GetPropertyValue("email.value")
        '                thisPerson.Key_GUID = p.GetPropertyValue("authentication.key_guid.value")
        '                thisPerson.mpdCountryId = the_country.mpdCountryId
        '                thisPerson.gr_person_id = p.ID

        '                d.Ap_mpd_Users.InsertOnSubmit(thisPerson)
        '                d.SubmitChanges()
        '            End If

        '            'Get Measurements for this person

        '            Dim fin_measures = gr_min.GetMeasurements(p.ID, Today.AddMonths(-13).ToString("yyyy-MM"), Today.ToString("yyyy-MM"), "", "Finance") ' need to specify Finance as Category, when implements
        '            Dim mpd_measures = gr_min.GetMeasurements(p.ID, Today.AddMonths(-13).ToString("yyyy-MM"), Today.ToString("yyyy-MM"), "", "MPD") ' need to specify MPD as Category, when implements

        '            For i As Integer = 0 To 13
        '                Dim thisMonth As AP_mpd_UserAccountInfo
        '                Dim checkDetail = From c In thisPerson.AP_mpd_UserAccountInfos Where c.period = Today.AddMonths(-i).ToString("yyyyMM")

        '                If checkDetail.Count > 0 Then
        '                    thisMonth = checkDetail.First
        '                Else
        '                    thisMonth = New AP_mpd_UserAccountInfo
        '                    thisMonth.mpdCountryId = the_country.mpdCountryId
        '                    thisMonth.mpdUserId = thisPerson.AP_mpd_UserId
        '                    thisMonth.period = Today.AddMonths(-i).ToString("yyyyMM")

        '                End If

        '                thisMonth.balance = GetValueFromMeasurementType(StaffBal, Today.AddMonths(-i).ToString("yyyy-MM"), fin_measures)
        '                thisMonth.income = GetValueFromMeasurementType(StaffIncLoc, Today.AddMonths(-i).ToString("yyyy-MM"), fin_measures)
        '                thisMonth.foreignIncome = GetValueFromMeasurementType(StaffIncFor, Today.AddMonths(-i).ToString("yyyy-MM"), fin_measures)
        '                thisMonth.compensation = GetValueFromMeasurementType(StaffIncSub, Today.AddMonths(-i).ToString("yyyy-MM"), fin_measures)
        '                thisMonth.expense = GetValueFromMeasurementType(StaffExp, Today.AddMonths(-i).ToString("yyyy-MM"), fin_measures)
        '                thisMonth.expBudget = GetValueFromMeasurementType(ExpenseBudget, Today.AddMonths(-i).ToString("yyyy-MM"), mpd_measures)
        '                thisMonth.toRaiseBudget = GetValueFromMeasurementType(ToRaiseBudget, Today.AddMonths(-i).ToString("yyyy-MM"), mpd_measures)

        '                If checkDetail.Count = 0 Then
        '                    d.AP_mpd_UserAccountInfos.InsertOnSubmit(thisMonth)
        '                End If



        '            Next
        '            'Update the User Specs
        '            d.SubmitChanges()
        '            Dim userAvg = (From c In d.AP_mpd_UserAccountInfos Where c.mpdUserId = thisPerson.AP_mpd_UserId And Not (c.balance = 0 And c.expense = 0 And c.income = 0) Order By c.period Descending).Take(12)
        '            If userAvg.Count > 0 Then
        '                thisPerson.AvgExpenses = userAvg.Average(Function(c) c.expense)

        '                thisPerson.AvgExpenseBudget12 = userAvg.Average(Function(c) c.expBudget)
        '                thisPerson.AvgMPDBudget12 = userAvg.Average(Function(c) c.toRaiseBudget)



        '                thisPerson.AvgIncome12 = (From c In userAvg Select (c.income + c.foreignIncome)).Average
        '                thisPerson.AvgIncome3 = (From c In userAvg.Take(3) Select (c.income + c.foreignIncome)).Average
        '                thisPerson.AvgIncome1 = (From c In userAvg.Take(1) Select (c.income + c.foreignIncome)).Average
        '                If thisPerson.AvgMPDBudget12 > 0 Then
        '                    thisPerson.AvgSupLevel12 = (From c In userAvg Where c.toRaiseBudget > 0 Select (c.income + c.foreignIncome) / c.toRaiseBudget).Average()
        '                    thisPerson.AvgSupLevel3 = (From c In userAvg.Take(3) Where c.toRaiseBudget > 0 Select (c.income + c.foreignIncome) / c.toRaiseBudget).Average
        '                    thisPerson.AvgSupLevel1 = (From c In userAvg Where c.toRaiseBudget > 0 And c.income + c.foreignIncome > 0).Select(Function(c) (c.income + c.foreignIncome) / c.toRaiseBudget).FirstOrDefault
        '                Else
        '                    thisPerson.AvgSupLevel12 = 1
        '                    thisPerson.AvgSupLevel3 = 1
        '                    thisPerson.AvgSupLevel1 = 1
        '                End If

        '                thisPerson.ForeignIncome12 = userAvg.Average(Function(c) c.foreignIncome)
        '                thisPerson.LocalIncome12 = userAvg.Average(Function(c) c.income)
        '                thisPerson.SubsidyIncome12 = userAvg.Average(Function(c) c.compensation)

        '                thisPerson.AvgIncome = thisPerson.ForeignIncome12 + thisPerson.LocalIncome12 + thisPerson.SubsidyIncome12
        '                If thisPerson.AvgIncome > 0 Then
        '                    thisPerson.SplitLocal = userAvg.Average(Function(c) c.income) / thisPerson.AvgIncome
        '                    thisPerson.SplitForeign = userAvg.Average(Function(c) c.foreignIncome) / thisPerson.AvgIncome
        '                    thisPerson.SplitSubsidy = userAvg.Average(Function(c) c.compensation) / thisPerson.AvgIncome
        '                Else
        '                    thisPerson.SplitLocal = 1
        '                    thisPerson.SplitForeign = 0
        '                    thisPerson.SplitSubsidy = 0
        '                End If

        '            Else
        '                thisPerson.AvgExpenses = 0
        '                thisPerson.AvgExpenseBudget12 = 0
        '                thisPerson.AvgMPDBudget12 = 0
        '                thisPerson.SplitLocal = 1
        '                thisPerson.SplitForeign = 0
        '                thisPerson.SplitSubsidy = 0
        '                thisPerson.AvgSupLevel12 = 1
        '                thisPerson.AvgSupLevel3 = 1
        '                thisPerson.AvgSupLevel1 = 1
        '                thisPerson.AvgIncome12 = 0
        '                thisPerson.AvgIncome3 = 0
        '                thisPerson.AvgIncome1 = 0
        '                thisPerson.ForeignIncome12 = 0
        '                thisPerson.LocalIncome12 = 0
        '                thisPerson.SubsidyIncome12 = 0

        '            End If




        '        Next
        '        d.SubmitChanges()

        '        Dim countryAvg = From c In d.Ap_mpd_Users Where c.mpdCountryId = the_country.mpdCountryId

        '        the_country.VeryLowCount = (From c In d.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 < 0.5).Count
        '        the_country.LowCount = (From c In d.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 0.5 And c.AvgSupLevel12 < 0.8).Count
        '        the_country.HighCount = (From c In d.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 0.8 And c.AvgSupLevel12 < 1.0).Count
        '        the_country.FullCount = (From c In d.Ap_mpd_Users Where c.AvgExpenseBudget12 > 0 And c.AvgSupLevel12 >= 1.0).Count
        '        If (countryAvg.Count > 0) Then

        '            Dim total_avg_income = countryAvg.Sum(Function(c) c.AvgIncome)
        '            the_country.SplitLocal = countryAvg.Sum(Function(c) c.LocalIncome12) / total_avg_income
        '            the_country.SplitForeign = countryAvg.Sum(Function(c) c.ForeignIncome12) / total_avg_income
        '            the_country.SplitSubsidy = countryAvg.Sum(Function(c) c.SubsidyIncome12) / total_avg_income

        '            the_country.NoBudgetCount = countryAvg.Count(Function(c) c.AvgExpenseBudget12 = 0)

        '            the_country.BudgetAccuracy = countryAvg.Where(Function(c) c.AvgExpenseBudget12 > 0).Average(Function(c) c.AvgExpenses / c.AvgExpenseBudget12)

        '            the_country.AvgSupport12 = countryAvg.Average(Function(c) c.AvgSupLevel12)
        '            the_country.AvgSupport3 = countryAvg.Average(Function(c) c.AvgSupLevel3)
        '            the_country.AvgSupport1 = countryAvg.Average(Function(c) c.AvgSupLevel1)


        '        Else
        '            the_country.SplitLocal = 1
        '            the_country.SplitForeign = 0
        '            the_country.SplitSubsidy = 0

        '            the_country.NoBudgetCount = 0

        '            the_country.BudgetAccuracy = 1
        '            the_country.AvgSupport12 = 0
        '            the_country.AvgSupport3 = 0
        '            the_country.AvgSupport1 = 0
        '        End If





        '    End If

        '    'update the country stats





        'Next

        'd.SubmitChanges()






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


    Private Function syncPerson(ByVal s As AP_StaffBroker_Staff, ByVal u As User, ByVal Portalid As Integer, ByRef gr As GR, ByVal ministry_id As String) As String
        Dim has_data As Boolean = False

        WriteLog("Sync " & u.DisplayName)
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

        p.ID = gr_id
        p.AddPropertyValue("ministry:relationship.ministry", ministry_id)
        p.AddPropertyValue("ministry:relationship.client_integration_id", u.UserID & "_" & Portalid)
        p.AddPropertyValue("authentication.key_guid", GetUserProperty("ssoGUID", u, Nothing))
        p.AddPropertyValue("authentication.client_integration_id", GetUserProperty("ssoGUID", u, Nothing))
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
        WriteLog("Push " & u.DisplayName)
       


        If p.profileProperties.Keys.Where(Function(c) Not excludeList.Contains(c)).Count = 0 Then
            If p.collections.Count = 1 Then
                If p.collections.Values.First.First.profileProperties.Keys.Where(Function(c) Not excludeList.Contains(c)).Count = 0 Then
                    Return gr_id
                End If
            End If
        End If



        gr_id = gr.UpdateEntity(p, "person")

        WriteLog("Pushed " & gr_id)
        Dim rel_id = GetProfileProperty(u.UserProfiles, "gr_ministry_membership_id", Nothing)

        If String.IsNullOrEmpty(rel_id) Then
            Dim the_person = gr.GetEntity(gr_id)
            SetProfileProperty(u.UserID, Portalid, "gr_ministry_membership_id", the_person.GetPropertyValue("ministry:relationship.relationship_entity_id"))

        End If




        'If Not tnt Is Nothing Then
        '    tnt.pushSummariesForStaffmember(GetStaffProperty("R/C", s), gr, gr_id, m)
        'End If

        'WriteLog("pushed fin Summaries " & u.DisplayName)


        ''sync the mpd data
        'Dim q = From c In d.Ap_mpd_Users Where c.StaffId = s.StaffId

        'For i As Integer = -14 To 0
        '    Dim period_1 = Today.AddMonths(i).ToString("yyyyMM")
        '    Dim period_2 = Today.AddMonths(i).ToString("yyyy-MM")
        '    Dim sb = getBudgetForStaffPeriod(s.StaffId, period_1)
        '    If Not sb Is Nothing Then
        '        If Not sb.TotalBudget Is Nothing Then
        '            m("ExpenseBudget").addMeasurement(gr_id, period_2, sb.TotalBudget)
        '            m("ToRaiseBudget").addMeasurement(gr_id, period_2, sb.ToRaise)

        '            'gr.AddUpdateMeasurement(gr_id, ExpenseBudget, period_2, sb.TotalBudget)
        '            'gr.AddUpdateMeasurement(gr_id, ToRaiseBudget, period_2, sb.ToRaise)
        '        End If

        '    End If
        'Next

        'WriteLog("pushed mpd Summaries " & u.DisplayName)


        Return gr_id

        'p.AddPropertyValue("address[1].city", GetProfileProperty(u.UserProfiles, "City"))
        'p.AddPropertyValue("address[1].country", GetProfileProperty(u.UserProfiles, "Country"))
        'p.AddPropertyValue("email_address[1].email", u.Username.TrimEnd(Portalid.ToString))


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
