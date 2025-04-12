Imports System.Collections.Concurrent
Imports IBM.Data.Db2
Imports ParameterMarkers.Exception

Namespace DB2Metadata

    Public Class Db2MetadataHelper
        Implements IDb2MetadataHelper
    
        Private ReadOnly _connectionString As String
        'Mettre en cache les métadonnées des colonnes pour éviter les recherches répétées
        Private ReadOnly _columnMetadataCache As New ConcurrentDictionary(Of String, List(Of Db2ColumnMetadata))
        private _columnMetadata As List(Of Db2ColumnMetadata)

    
        Public Sub New(connectionString As String)
            If String.IsNullOrWhiteSpace(connectionString) Then
                Throw New ArgumentException("La chaîne de connexion ne peut pas être vide", NameOf(connectionString))
            End If
            _connectionString = connectionString
        End Sub
    
        'Obtenir les métadonnées de colonne pour une table spécifique
        Public Function GetColumnMetadata(schemaName As String, tableName As String) As List(Of Db2ColumnMetadata) Implements IDb2MetadataHelper.GetColumnMetadata
            If String.IsNullOrWhiteSpace(schemaName) OrElse String.IsNullOrWhiteSpace(tableName) Then
                Throw New ArgumentException("Le nom du schéma et le nom de la table ne doivent pas être vides")
            End If

            Dim cacheKey As String = $"{schemaName.ToUpper()}.{tableName.ToUpper()}"
        
            _columnMetadata = _columnMetadataCache.GetOrAdd(cacheKey, Function(key) LoadColumnMetadata(schemaName, tableName))
            return _columnMetadata
        End Function
        
        ' Convertir le type de données DB2 en énumération DB2Type
        Public  Function GetDB2Type(dataType As String) As DB2Type Implements IDb2MetadataHelper.GetDB2Type
            Select Case dataType.Trim().ToUpper()
                Case "VARCHAR", "VARGRAPHIC", "LONGVAR"
                    Return DB2Type.VarChar
                Case "CHAR", "GRAPHIC"
                    Return DB2Type.Char
                Case "INTEGER", "INT"
                    Return DB2Type.Integer
                Case "SMALLINT"
                    Return DB2Type.SmallInt
                Case "BIGINT"
                    Return DB2Type.BigInt
                Case "DECIMAL", "DEC", "NUMERIC", "NUM"
                    Return DB2Type.Decimal
                Case "REAL", "FLOAT"
                    Return DB2Type.Real
                Case "DOUBLE"
                    Return DB2Type.Double
                Case "DATE"
                    Return DB2Type.Date
                Case "TIME"
                    Return DB2Type.Time
                Case "TIMESTAMP"
                    Return DB2Type.Timestamp
                Case "BLOB"
                    Return DB2Type.Blob
                Case "CLOB"
                    Return DB2Type.Clob
                Case "DBCLOB"
                    Return DB2Type.DbClob
                Case Else
                    Return DB2Type.VarChar
            End Select
        End Function

        Private Function LoadColumnMetadata(schemaName As String, tableName As String) As List(Of Db2ColumnMetadata)
            Try
                Return LoadColumnMetadataInternal(schemaName, tableName)
            Catch ex As DB2Exception
                Throw New DatabaseException($"Failed to get metadata for {schemaName}.{tableName}", ex)
            End Try
        End Function
        
        Private Function LoadColumnMetadataInternal(schemaName As String, tableName As String) As List(Of Db2ColumnMetadata)
            Dim columns As New List(Of Db2ColumnMetadata)
        
            Using conn As New DB2Connection(_connectionString)
                conn.Open()
            
                Dim sql As String = "
                SELECT 
                    NAME, 
                    COLTYPE, 
                    LENGTH, 
                    SCALE, 
                    NULLS,
                    COLNO
                FROM 
                    SYSIBM.SYSCOLUMNS 
                WHERE 
                    TBCREATOR = @SchemaName AND 
                    TBNAME = @TableName
                ORDER BY 
                    COLNO"
            
                Using cmd As New DB2Command(sql, conn)
                    cmd.Parameters.Add("@SchemaName", DB2Type.VarChar).Value = schemaName.ToUpper()
                    cmd.Parameters.Add("@TableName", DB2Type.VarChar).Value = tableName.ToUpper()
                
                    Using reader As DB2DataReader = cmd.ExecuteReader()
                        While reader.Read()
                            columns.Add(New Db2ColumnMetadata() With {
                                           .ColumnName = reader("NAME").ToString(),
                                           .DataType = reader("COLTYPE").ToString(),
                                           .Length = Convert.ToInt32(reader("LENGTH")),
                                           .Scale = Convert.ToInt32(reader("SCALE")),
                                           .Nullable = reader("NULLS").ToString() = "Y"
                                           })
                        End While
                    End Using
                End Using
            End Using
        
            Return columns
        End Function
        
        Public Function SetParameterWithMetadata(cmd As DB2Command, paramName As String, columnName As String, value As Object) As DB2Parameter Implements IDb2MetadataHelper.SetParameterWithMetadata
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
        Dim db2Type As DB2Type = GetDB2Type(metadata.DataType)
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
   End Class
End NameSpace