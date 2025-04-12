Namespace Repository
    Public MustInherit Class DB2RepositoryBase(Of T)
        Protected ReadOnly ConnectionString As String

        Public Sub New(connectionString As String)
            Me.ConnectionString = connectionString
        End Sub
    End Class
End NameSpace