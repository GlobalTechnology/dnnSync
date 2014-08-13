Imports Google.GData.Client
Imports Google.GData.Spreadsheets
Imports GR_NET
Imports System.Configuration
Public Class GoogleDrive
    Shared Sub SyncValues(Optional ByVal Stage As Boolean = False)
     
        Dim gr As GR
        If Stage Then

            gr = New GR(ConfigurationManager.AppSettings("gma_status_stage_key"), ConfigurationManager.AppSettings("gr_stage_server"))
        Else
            gr = New GR(ConfigurationManager.AppSettings("gma_status_api_key"), ConfigurationManager.AppSettings("gr_server"))
        End If

        
        Dim service As New SpreadsheetsService("MySpreadsheetIntegration-v1")
        service.setUserCredentials(ConfigurationManager.AppSettings("google_username"), ConfigurationManager.AppSettings("google_password"))
        Dim query As New SpreadsheetQuery(ConfigurationManager.AppSettings("google_query_gma"))


        Dim feed As SpreadsheetFeed = service.Query(query)


        Dim ministry = gr.GetEntities("ministry", "&filters[owned_by]=a6c9eb6c-d554-11e3-a415-12725f8f377c", 0, 500)


        ' TODO: There were no spreadsheets, act accordingly.
        If feed.Entries.Count = 1 Then
            Dim spreadsheet As SpreadsheetEntry = DirectCast(feed.Entries(0), SpreadsheetEntry)
            Dim wsFeed As WorksheetFeed = spreadsheet.Worksheets
            Dim worksheet As WorksheetEntry = DirectCast(wsFeed.Entries(0), WorksheetEntry)
            Dim listFeedLink As AtomLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, Nothing)
            Dim listQuery As ListQuery = New ListQuery(listFeedLink.HRef.ToString())

            'Dim listFeed = service.Query(listQuery)


            'For Each row As ListEntry In ListFeed.Entries
            '    Console.WriteLine(row.Title.Text)
            '    For Each col As ListEntry.Custom In row.Elements
            '        Console.WriteLine(col.Value)
            '    Next
            'Next
            Dim cellQuery = New CellQuery(worksheet.CellFeedLink)
            cellQuery.MinimumRow = 11
            Dim cellFeed = service.Query(cellQuery)
            For I As Integer = 11 To worksheet.RowCount.Count
                Dim row = cellFeed.Entries.Where(Function(c) c.Title.Text.Substring(1) = I)

                If row.Count > 0 Then
                    Dim code = getCellEntry(row, "R" & I)


                    If Not String.IsNullOrEmpty(code) Then
                        'We have a min_code
                        Dim SLM = getCellEntry(row, "M" & I).ToLower
                        Dim GCM = getCellEntry(row, "N" & I).ToLower
                        Dim LLM = getCellEntry(row, "O" & I).ToLower
                        Dim DS = getCellEntry(row, "P" & I).ToLower
                        Dim Answer = "Not Implemented"
                        If SLM = "r" Or GCM = "r" Or LLM = "r" Or DS = "r" Then
                            Answer = "Reporting"
                        ElseIf SLM = "i" Or GCM = "i" Or LLM = "i" Or DS = "i" Then
                            Answer = "Implemented"

                        End If
                        Module1.Log(code & ": " & Answer & vbNewLine)
                        Dim min = From c In ministry Where c.GetPropertyValue("min_code") = code

                        If min.Count > 0 Then
                            Dim insert As New GR_NET.Entity()
                            insert.ID = min.First.ID
                            insert.AddPropertyValue("gma_status", Answer)
                            insert.AddPropertyValue("client_integration_id", code)

                            gr.UpdateEntity(insert, "ministry")




                        End If


                    End If
                End If
            Next
            'For Each row As CellEntry In cellFeed.Entries

            '    Console.Write(row.Title.Text & " - " & row.Value & vbNewLine)
            'Next


        End If





    End Sub


    Shared Sub SyncStages(Optional ByVal isStage As Boolean = False)

        Dim gr As GR
        If isStage Then

            gr = New GR(ConfigurationManager.AppSettings("gma_status_stage_key"), ConfigurationManager.AppSettings("gr_stage_server"))
        Else
            gr = New GR(ConfigurationManager.AppSettings("gma_status_api_key"), ConfigurationManager.AppSettings("gr_server"))
        End If


        Dim service As New SpreadsheetsService("MySpreadsheetIntegration-v1")

        service.setUserCredentials(ConfigurationManager.AppSettings("google_username"), ConfigurationManager.AppSettings("google_password"))

        Dim query As New SpreadsheetQuery(ConfigurationManager.AppSettings("google_query_stages"))

        'Dim query As New SpreadsheetQuery()

        Dim feed As SpreadsheetFeed = service.Query(query)


        ' Dim ministry = gr.GetEntities("ministry", "&filters[owned_by]=a6c9eb6c-d554-11e3-a415-12725f8f377c", 0, 500)


        'For Each row In feed.Entries.Where(Function(c) c.Title.Text.Contains("Stages"))
        '    Console.WriteLine(row.SelfUri)
        'Next
        ' TODO: There were no spreadsheets, act accordingly.
        If feed.Entries.Count = 1 Then
            Dim spreadsheet As SpreadsheetEntry = DirectCast(feed.Entries(0), SpreadsheetEntry)
            Dim wsFeed As WorksheetFeed = spreadsheet.Worksheets
            Dim worksheet As WorksheetEntry = DirectCast(wsFeed.Entries(0), WorksheetEntry)
            Dim listFeedLink As AtomLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, Nothing)
            Dim listQuery As ListQuery = New ListQuery(listFeedLink.HRef.ToString())

            'Dim listFeed = service.Query(listQuery)


            'For Each row As ListEntry In listFeed.Entries
            '    Console.WriteLine(row.Title.Text)
            '    For Each col As ListEntry.Custom In row.Elements
            '        Console.WriteLine(col.Value)
            '    Next
            'Next
            Dim cellQuery = New CellQuery(worksheet.CellFeedLink)
            cellQuery.MinimumRow = 2
            Dim cellFeed = service.Query(cellQuery)
            For I As Integer = 2 To worksheet.RowCount.Count
                Dim row = cellFeed.Entries.Where(Function(c) c.Title.Text.Substring(1) = I)

                If row.Count > 0 Then
                    Dim min_id As String = getCellEntry(row, "N" & I)


                    If (Not String.IsNullOrEmpty(min_id)) And min_id <> "#N/A" Then

                        'We have a min_code
                        Dim SLM = (getCellEntry(row, "I" & I).ToLower = "1").ToString.ToLower
                        Dim GCM = (getCellEntry(row, "J" & I).ToLower = "1").ToString.ToLower
                        Dim LLM = (getCellEntry(row, "K" & I).ToLower = "1").ToString.ToLower
                        Dim DS = (getCellEntry(row, "L" & I).ToLower = "1").ToString.ToLower
                        Dim Stage = getCellEntry(row, "D" & I).ToLower

                        Module1.Log(getCellEntry(row, "C" & I) & ": " & Stage & vbNewLine)

                        Dim insert As New GR_NET.Entity()
                        insert.ID = min_id
                        insert.AddPropertyValue("stage", Stage)
                        insert.AddPropertyValue("has_slm", SLM)
                        insert.AddPropertyValue("has_gcm", GCM)
                        insert.AddPropertyValue("has_llm", LLM)
                        insert.AddPropertyValue("has_ds", DS)
                        insert.AddPropertyValue("client_integration_id", min_id)
                        gr.UpdateEntity(insert, "ministry")



                            

                    End If
                End If
            Next
            'For Each row As CellEntry In cellFeed.Entries

            '    Console.Write(row.Title.Text & " - " & row.Value & vbNewLine)
            'Next


        End If





    End Sub

    Private Shared Function getCellEntry(ByVal cellEntry As IEnumerable(Of AtomEntry), ByVal Cell As String) As String
        Dim rtn = (From c As CellEntry In cellEntry Where c.Title.Text = Cell Select c.Value).FirstOrDefault
        If Not rtn Is Nothing Then
            Return rtn
        Else
            Return ""
        End If
    End Function
End Class
