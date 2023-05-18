using Microsoft.AspNetCore.Http;
using libcdm;
public class CDMIngresoDesdeCdm
{
    private readonly RequestDelegate _next;
    private readonly string _ruta;

    private readonly CDMApiOperaciones _api;


    public CDMIngresoDesdeCdm(RequestDelegate next, string ruta, CDMApiOperaciones api)
    {
        _next = next;
        _ruta = ruta;
        _api = api;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == _ruta)
        {
            try
            {
                if (context.Request.Query.ContainsKey("idSesion")){
                    var middlewareAutoriza = new CDMAutoriza(_next,"MiddlewareVerificaSesionToken",_api);
                    await middlewareAutoriza.InvokeAsync(context);
                    return;
                }
            }
            catch (System.Exception)
            {
                await _next(context);

            }
        }
        await _next(context);
    }
}