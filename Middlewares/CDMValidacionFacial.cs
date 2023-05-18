using Microsoft.AspNetCore.Http;
using libcdm;
public class CDMValidacionFacial
{
    private readonly RequestDelegate _next;
    private readonly CDMOpcionesMiddleware _opciones;

    private readonly CDMApiOperaciones _api;


    public CDMValidacionFacial(RequestDelegate next, CDMOpcionesMiddleware opciones, CDMApiOperaciones api)
    {
        _next = next;
        _opciones = opciones;
        _api = api;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (Array.Exists(_opciones.rutas!, element => element == context.Request.Path))
        {
            CDMRequest Requestcdm = (CDMRequest)context.Items["cdm"]!;
            var solicitud = context.Request.Cookies["cdm-solicitud-facetec"];
            if (_opciones.ventanaVida == false)
            {   
                solicitud = context.Request.Query["solicitud"].ToString();
            }
            if (_opciones.rutaError == "")
            {
                throw new Exception("No se ha configurado la ruta de error");
            }
            try
                {
                    if(solicitud != ""){
                        CDMApiResponse rta = await _api.api_getSolicitud(context.Request.Cookies["cdm-token"] ?? "", solicitud!);
                        if(rta.status == 200){
                            if(rta.data!["resultadoProceso"] != "correcto"){
                                context.Response.Cookies.Delete("cdm-solicitud-facetec");
                                context.Response.Redirect(_opciones.rutaError+ "?solicitud=" + solicitud);
                                return;
                            }else{
                                Requestcdm!.status = rta.status;
                                Requestcdm!.msg = rta.msg;
                                Requestcdm!.data!.Add("validacionFacial", rta);
                                Requestcdm!.data!["validacionFacial"].data.Add("solicitud", solicitud);
                            await _next(context);
                                return;
                            }
                        }
                        context.Response.Cookies.Delete("cdm-solicitud-facetec");
                    }
                    string originalUrl = context.Request.Path + context.Request.QueryString;
                    context.Items["originUrl"] = originalUrl;        
                    CDMApiResponse rtaValidaOTP =  await _api.api_validaFaceTec(context.Request.Cookies["cdm-token"] ?? "",(string)context.Items["originalUrl"]!);
                    if (rtaValidaOTP.msg == "redireccionado")
                    {
                        context.Response.Redirect(rtaValidaOTP.data?["Redirect"]);
                        return;
                    }
                    context.Response.Redirect(_opciones.rutaError+ "?solicitud=" + solicitud);
                    return;                       
            }
            catch (System.Exception)
            {
                context.Response.Redirect(_opciones.rutaError+ "?solicitud=" + solicitud);
            }
        }
        await _next(context);
    }
}