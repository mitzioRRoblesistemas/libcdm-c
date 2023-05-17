using Microsoft.AspNetCore.Http;
using libcdm;
public class CDMAutoriza
{
    private readonly RequestDelegate _next;
    private readonly string _ruta;

    private readonly CDMApiOperaciones _api;


    public CDMAutoriza(RequestDelegate next, string rutaLogin, CDMApiOperaciones api)
    {
        _next = next;
        _ruta = rutaLogin;
        _api = api;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if ("MiddlewareVerificaSesionToken" == _ruta || context.Request.Path == _ruta)
        {
            try
            {
                var session = context.Request.Query.ContainsKey("idSesion") ? context.Request.Query["idSesion"].ToString() : context.Request.Cookies["cdm-idSesion"];
                CDMApiResponse rta = await _api.ApiAutorizaAsync(context.Request.Cookies["cdm-keyLogin"] ?? "", session!, (string)context.Items["originUrl"]! ?? "/");
                if(rta.msg == "redireccionado"){
                    context.Response.Redirect(rta.data?["Redirect"]);
                    return;
                }
                CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                Requestcdm!.status = rta.status;
                Requestcdm!.msg = rta.msg;
                Requestcdm!.data!.Add("autoriza", rta);
                context.Items["cdm"] = Requestcdm;    
            }
            catch (System.Exception)
            {
                CDMRequest cdm = new CDMRequest();
                CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                cdm.status = 500;
                cdm.msg ="Error inesperado";
                Requestcdm!.status = cdm.status;
                Requestcdm!.msg = cdm.msg;
                Requestcdm!.data!.Add("autoriza", cdm);
                context.Items["cdm"] = Requestcdm;    
            }
        }
        await _next(context);
    }
}