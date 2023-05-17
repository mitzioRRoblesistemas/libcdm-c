using Microsoft.AspNetCore.Http;
using libcdm;
public class CDMVerificaSesionToken
{
    private readonly RequestDelegate _next;
    private readonly CDMOpcionesMiddleware _ruta;

    private readonly CDMApiOperaciones _api;


    public CDMVerificaSesionToken(RequestDelegate next, CDMOpcionesMiddleware rutas, CDMApiOperaciones api)
    {
        _next = next;
        _ruta = rutas;
        _api = api;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (Array.Exists(_ruta.rutas!, element => element == context.Request.Path))
        {
            string originalUrl = context.Request.Path + context.Request.QueryString;
            context.Items["originUrl"] = originalUrl;    
            try
            {
                var middlewareAutoriza = new CDMAutoriza(_next,"MiddlewareVerificaSesionToken",_api);
                if(context.Request.Cookies["cdm-token"] != null && context.Request.Cookies["cdm-token"] != ""){
                    CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                    CDMApiResponse rta = await _api.api_verificaSesionToken(context.Request.Cookies["cdm-token"] ?? "");                    
                    if (rta.status != 200)
                    {       
                        await middlewareAutoriza.InvokeAsync(context);
                        return;
                    }
                    Requestcdm!.status = rta.status;
                    Requestcdm!.msg = rta.msg;
                    Requestcdm.idColeccion = rta.data!["idColeccion"];
                    Requestcdm!.data!.Add("verificaSesionToken", rta);
                    context.Items["cdm"] = Requestcdm;
                    await _next(context);    
                    return;
                }
                await middlewareAutoriza.InvokeAsync(context);
                return;
            }
            catch (System.Exception)
            {
                CDMRequest cdm = new CDMRequest();
                CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
                cdm.status = 500;
                cdm.msg ="Error inesperado";
                Requestcdm!.status = cdm.status;
                Requestcdm!.msg = cdm.msg;
                Requestcdm!.data!.Add("verificaSesionToken", cdm);
                context.Items["cdm"] = Requestcdm;    
            }
        }
        await _next(context);
    }
}