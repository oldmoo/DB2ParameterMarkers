Imports System.Data
Imports IBM.Data.Db2
Imports ParameterMarkers.DB2Metadata

Namespace Repository
   Public Class DB2RepositoryWithMetadata(Of T)
    Inherits DB2RepositoryBase(Of T)
       implements IDisposable
    
    Protected ReadOnly _metadataHelper As Db2MetadataHelper
    Protected ReadOnly _schemaName As String
    Protected ReadOnly _tableName As String
    Protected _columnMetadata As List(Of DB2ColumnMetadata)
    Private _disposed As Boolean = False

    
    Public Sub New(connectionString As String, schemaName As String, tableName As String)
        MyBase.New(connectionString)
        _metadataHelper = New Db2MetadataHelper(connectionString)
        _schemaName = schemaName
        _tableName = tableName
        _columnMetadata = _metadataHelper.GetColumnMetadata(schemaName, tableName)
    End Sub
    
    ' Définissez un paramètre avec la taille correcte en fonction des métadonnées de la colonne
    Protected Function SetParameterWithMetadata(cmd As DB2Command, paramName As String, columnName As String, value As Object) As DB2Parameter
        If cmd Is Nothing Then
            Throw New ArgumentNullException(NameOf(cmd))
        End If
    
        If String.IsNullOrWhiteSpace(paramName) Then
            Throw New ArgumentException("le nom du paramètre ne peut pas être vide", NameOf(paramName))
        End If
    
        If String.IsNullOrWhiteSpace(columnName) Then
            Throw New ArgumentException("le nom du paramètre ne peut pas être vide", NameOf(columnName))
        End If
        
        ' Supprimez le préfixe @ ou : du nom du paramètre pour qu'il corresponde au nom de la colonne
        Dim cleanParamName As String = columnName
        If columnName.StartsWith("@c") OrElse columnName.StartsWith(":c") Then
            cleanParamName = columnName.Substring(1)
        End If
        
        ' Find column metadata
        Dim metadata = _columnMetadata.FirstOrDefault(Function(c) c.ColumnName.Equals(cleanParamName, StringComparison.OrdinalIgnoreCase))
        
'        If metadata Is Nothing Then
'            ' Retour à la gestion des paramètres par défaut si les métadonnées ne sont pas trouvées
'            Return cmd.Parameters.Add(paramName, GetDB2Type(value)).Value = If(value Is Nothing, DBNull.Value, value)
'        End If
        
        ' Créer un paramètre avec le type et la taille DB2 appropriés
        Dim db2Type As DB2Type = _metadataHelper.GetDB2Type(metadata.DataType)
        Dim param As DB2Parameter
        
        ' Définir la taille pour les types qui en ont besoin
        Select Case db2Type
            Case DB2Type.VarChar, DB2Type.Char, DB2Type.VarBinary, DB2Type.Binary
                param = cmd.Parameters.Add(paramName, db2Type, metadata.Length)
            Case DB2Type.Decimal
                ' For decimal, we need precision and scale
                param = cmd.Parameters.Add(paramName, db2Type)
                param.Precision = CByte(metadata.Length)
                param.Scale = CByte(metadata.Scale)
            Case Else
                param = cmd.Parameters.Add(paramName, db2Type)
        End Select
        
        param.Value = If(value Is Nothing AndAlso metadata.Nullable, DBNull.Value, value)
        Return param
    End Function
    
    ' Remplacement à utiliser lors de l'exécution de SQL avec des paramètres
    Protected Function ExecuteQueryWithMetadata(sql As String, parameters As Dictionary(Of String, Object)) As DataTable
        Dim result As New DataTable()
        
        Using conn As New DB2Connection(ConnectionString)
            conn.Open()
            
            Using cmd As New DB2Command(sql, conn)
                ' Ajouter tous les paramètres avec les métadonnées
                For Each param In parameters
                    SetParameterWithMetadata(cmd, param.Key, param.Key, param.Value)
                Next
                
                ' Remplir le tableau de données
                Using adapter As New DB2DataAdapter(cmd)
                    adapter.Fill(result)
                End Using
            End Using
        End Using
        
        Return result
    End Function

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                ' Éliminer les ressources gérées
                If TypeOf _metadataHelper Is IDisposable Then
                    DirectCast(_metadataHelper, IDisposable).Dispose()
                End If
            End If
            _disposed = True
        End If
    End Sub
    
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
   End Class
End NameSpace