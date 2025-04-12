Imports IBM.Data.Db2

Namespace DB2Metadata

    Public Interface IDb2MetadataHelper
        Function GetColumnMetadata(schemaName As String, tableName As String) As List(Of Db2ColumnMetadata)
        Function GetDB2Type(dataType As String) As DB2Type
        Function SetParameterWithMetadata(cmd As DB2Command, paramName As String, columnName As String, value As Object) As DB2Parameter
    End Interface
End NameSpace