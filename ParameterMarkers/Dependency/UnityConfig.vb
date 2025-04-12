
Imports Unity

Namespace Dependency

    Public NotInheritable Class UnityConfig
        Private Sub New()
        End Sub

        Public Shared Function Configure() As IUnityContainer
            Dim container As New UnityContainer()

            ' Register DB2MetadataHelper
'        container.RegisterType(Of IDB2MetadataHelper, DB2MetadataHelper)( _
'            New ContainerControlledLifetimeManager(), _
'            New InjectionConstructor(ConfigurationManager.ConnectionStrings("YourConnectionString").ConnectionString) _
'            )

            Return container
        End Function
    End Class

'Public Class WebApiApplication
'    Inherits System.Web.HttpApplication
'
'    Protected Sub Application_Start()
'        ' If using Autofac
'        Dim container = ContainerConfig.Configure()
'        ' Or if using Unity
'        ' Dim container = UnityConfig.Configure()
'
'        ' Setup your Web API configuration here
'        GlobalConfiguration.Configure(AddressOf WebApiConfig.Register)
'    End Sub
'End Class
End NameSpace