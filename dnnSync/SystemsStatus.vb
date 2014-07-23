Imports GR_NET
Imports System.Configuration
Public Class SystemsStatus
    Shared Sub GetSystemsStatus(Optional ByVal Stage As Boolean = False)


        Dim gr As GR
        If Stage Then

            gr = New GR(ConfigurationManager.AppSettings("jon_test_stage_key"), ConfigurationManager.AppSettings("gr_stage_server"))
        Else
            gr = New GR(ConfigurationManager.AppSettings("jon_test_api_key"), ConfigurationManager.AppSettings("gr_server"))
        End If

       


        '  Dim ministries = gr.GetEntities("ministry", "&filters[owned_by]=all&filters[ministry_scope][]=National&filters[ministry_scope][]=Area&filters[ministry_scope][]=National Region", 0, 2000)
        Dim ministries = gr.GetEntities("ministry", "&ruleset=global_minisries", 0, 2000)
        'ministries.AddRange(gr.GetEntities("ministry", "&filters[owned_by]=all&filters[ministry_scope]=Area", 0, 2000))
        'ministries.AddRange(gr.GetEntities("ministry", "&filters[owned_by]=all&filters[ministry_scope]=National Region", 0, 2000))

        Dim areas = gr.GetEntities("area", "")
        Dim country = gr.GetEntities("iso_country", "", 0, 500)
        'Dim scopes As String() = {"National", "National_Region", "Area"}

        Dim d As New AgapeConnectDataContext
        For Each min In ministries
            Console.Write(min.GetPropertyValue("name"))
            Dim q = From c In d.ministry_systems Where c.min_code = min.GetPropertyValue("min_code")
            Dim area_id = min.GetPropertyValue("area:relationship.area")
            Dim area = From c In areas Where c.ID = area_id Select name = c.GetPropertyValue("area_name"), code = c.GetPropertyValue("area_code")
            Dim area_name = ""
            Dim area_code = ""
            If area.Count > 0 Then
                area_name = area.First.name
                area_code = area.First.code
            End If

            Dim iso = (From c In country Where c.ID = min.GetPropertyValue("iso_country:relationship.iso_country") Select c.GetPropertyValue("iso2_code")).FirstOrDefault

            Dim hard_code = {"USA", "CAN"}
            If q.Count > 0 Then
                set_if(q.First.last_dataserver_donation, min.GetPropertyValue("last_dataserver_donation"), "Date")
                set_if(q.First.last_dataserver_transaction, min.GetPropertyValue("last_dataserver_transaction"), "Date")
                set_if(q.First.last_fin_rep, min.GetPropertyValue("last_financial_report"), "Period")
                set_if(q.First.gma_status, min.GetPropertyValue("gma_status"), "ImplementationStatus")
                set_if(q.First.min_logo, min.GetPropertyValue("logo_url"))
                set_if(q.First.min_code, min.GetPropertyValue("min_code"))
                set_if(q.First.min_name, min.GetPropertyValue("name"))
                set_if(q.First.stage, min.GetPropertyValue("stage"))
                set_if(q.First.fcx, min.GetPropertyValue("is_fcx"))
                set_if(q.First.ministry_scope, min.GetPropertyValue("ministry_scope"))
                set_if(q.First.is_active, min.GetPropertyValue("is_active"), "Boolean")
                set_if(q.First.area_code, area_code)
                set_if(q.First.area_name, area_name)
                set_if(q.First.iso2_code, iso)
                If hard_code.Contains(q.First.min_code) Then
                    set_if(q.First.last_dataserver_donation, Today, "Date")
                End If

            Else
                Dim insert As New ministry_system
                set_if(insert.last_dataserver_donation, min.GetPropertyValue("last_dataserver_donation"), "Date")
                set_if(insert.last_dataserver_transaction, min.GetPropertyValue("last_dataserver_transaction"), "Date")
                set_if(insert.last_fin_rep, min.GetPropertyValue("last_financial_report"), "Period")
                set_if(insert.gma_status, min.GetPropertyValue("gma_status"), "ImplementationStatus")
                set_if(insert.min_logo, min.GetPropertyValue("logo_url"))
                set_if(insert.min_code, min.GetPropertyValue("min_code"))
                set_if(insert.min_name, min.GetPropertyValue("name"))
                set_if(insert.stage, min.GetPropertyValue("stage"))
                set_if(insert.fcx, min.GetPropertyValue("is_fcx"))
                set_if(insert.ministry_scope, min.GetPropertyValue("ministry_scope"))
                set_if(insert.is_active, min.GetPropertyValue("is_active"), "Boolean")
                set_if(insert.gr_ministry_id, min.ID)
                set_if(insert.area_code, area_code)
                set_if(insert.area_name, area_name)
                set_if(insert.iso2_code, iso)

                If hard_code.Contains(insert.min_code) Then
                    set_if(insert.last_dataserver_donation, Today, "Date")
                End If
                d.ministry_systems.InsertOnSubmit(insert)
            End If

        Next
        d.SubmitChanges()
    End Sub

    Private Shared Sub set_if(ByRef prop As Object, ByVal value As String, Optional ByVal T As String = "String")
        Try



            If Not String.IsNullOrEmpty(value) Then
                If T = "Date" Then
                    prop = DateTime.Parse(value, New System.Globalization.CultureInfo(""))
                ElseIf T = "Period" Then
                    prop = New Date(Left(value, 4), Right(value, 2), 1)
                ElseIf T = "Boolean" Then
                    prop = value.ToLower = "true"
                ElseIf T = "ImplementationStatus" Then
                    Select Case value
                        Case "Not Implemented"
                            prop = CByte(0)
                        Case "Implemented"
                            prop = CByte(1)
                        Case "Reporting"
                            prop = CByte(2)

                    End Select
                Else
                    prop = value
                End If

            Else
                If T = "Boolean" Then
                    prop = False
                Else



                    prop = Nothing
                End If
            End If
        Catch ex As Exception
            prop = Nothing
        End Try
    End Sub
End Class
