Namespace Exception
    Public Class DatabaseException
        Inherits System.Exception
    
        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    
        Public Sub New(message As String, innerException As System.Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class
End NameSpace