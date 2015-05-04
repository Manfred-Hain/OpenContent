#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using System.Linq;
using System.Collections.Generic;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Services.Localization;
using DotNetNuke.Security;
using DotNetNuke.Web.Razor;
using System.IO;
using DotNetNuke.Services.Exceptions;
using System.Web.UI;
using System.Web.Hosting;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using Newtonsoft.Json;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Web.Client.ClientResourceManagement;
using DotNetNuke.Web.Client;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Json;
using Handlebars;
using System.Web.WebPages;
using System.Web;

#endregion

namespace Satrabel.OpenContent
{
    
    public partial class View : RazorModuleBase, IActionable
    {
        public string TemplateFolder
        {
            get
            {
                return ModuleContext.PortalSettings.HomeSystemDirectory + "/OpenContent/Templates/";
            }
        }
        protected override string RazorScriptFile
        {
            get
            {
                // string m_RazorScriptFile = base.RazorScriptFile;
                var m_RazorScriptFile = "";
                string Template = ModuleContext.Settings["template"] as string;

                if (!(string.IsNullOrEmpty(Template)))
                {
                    m_RazorScriptFile = "~/" + Template;
                }
                return m_RazorScriptFile;
            }
        }

        
        protected override void OnPreRender(EventArgs e)
        {
            string OutputString = GenerateOutput();
            Controls.Add(new LiteralControl(Server.HtmlDecode(OutputString)));
        }


        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (!(string.IsNullOrEmpty(RazorScriptFile)))
            {
                //JavaScript.RequestRegistration() 
                string cssfilename = Path.ChangeExtension(RazorScriptFile, "css");

                if (File.Exists(HostingEnvironment.MapPath(cssfilename)))
                {
                    ClientResourceManager.RegisterStyleSheet(Page, Page.ResolveUrl(cssfilename), FileOrder.Css.PortalCss);
                }

                string jsfilename = Path.ChangeExtension(RazorScriptFile, "js");

                if (File.Exists(HostingEnvironment.MapPath(jsfilename)))
                {
                    ClientResourceManager.RegisterScript(Page, Page.ResolveUrl(jsfilename), FileOrder.Js.DefaultPriority);
                }
              

                
            }
        }

        private string GenerateOutput()
        {
            //List<string> scripts = new List<string>();

            //base.OnPreRender(e);
            try
            {
                if (!(string.IsNullOrEmpty(RazorScriptFile)))
                {
                    if (!File.Exists(Server.MapPath(RazorScriptFile)))
                        Exceptions.ProcessModuleLoadException(this, new Exception(RazorScriptFile + " don't exist"));


                    //string filename = HostingEnvironment.MapPath("~/DesktopModules/Satrabel/Content/Templates/Carousel/Data.json");
                    //string data = File.ReadAllText(filename);
                    OpenContentController ctrl = new OpenContentController();
                    var struc = ctrl.GetFirstContent(ModuleContext.ModuleId);
                    if (struc != null)
                    {
                        string Data = ModuleContext.Settings["data"] as string;

                        //dynamic json = JValue.Parse(struc.Json);
                        //JObject model = new JObject();
                        //model["Data"] = JValue.Parse(struc.Json);
                        //model["Settings"] = JValue.Parse(Data);

                        //dynamic model = new ExpandoObject();
                        //model.Data = JsonUtils.JsonToDynamic(struc.Json);

                        dynamic model = JsonUtils.JsonToDynamic(struc.Json);
                        model.Settings = JsonUtils.JsonToDynamic(Data);

                        if (Path.GetExtension(RazorScriptFile) != ".hbs")
                        {
                            string webConfig = Path.GetDirectoryName(Server.MapPath(RazorScriptFile));
                            webConfig = webConfig.Remove(webConfig.LastIndexOf("\\")) + "\\web.config";
                            if (!File.Exists(webConfig))
                            {
                                string filename = HostingEnvironment.MapPath("~/DesktopModules/OpenContent/Templates/web.config");
                                File.Copy(filename, webConfig);
                            }

                            var razorEngine = new RazorEngine(RazorScriptFile, ModuleContext, LocalResourceFile);
                            var writer = new StringWriter();
                            try
                            {
                                RazorRender(razorEngine.Webpage, writer, model);
                                //Controls.Add(new LiteralControl(Server.HtmlDecode(writer.ToString())));
                                return writer.ToString();
                            }
                            catch (Exception ex)
                            {
                                Exceptions.ProcessModuleLoadException(this, ex);
                                //Controls.Add(new LiteralControl(Server.HtmlDecode(writer.ToString())));
                            }


                        }
                        else
                        {
                            string source = File.ReadAllText(Server.MapPath(RazorScriptFile));
                            var hbs = Handlebars.Handlebars.Create();
                            hbs.RegisterHelper("multiply", (writer, context, parameters) =>
                            {
                                try
                                {
                                    int a = int.Parse(parameters[0].ToString());
                                    int b = int.Parse(parameters[1].ToString());
                                    int c = a * b;
                                    writer.WriteSafeString(c.ToString());

                                }
                                catch (Exception)
                                {
                                    writer.WriteSafeString("0");
                                }
                            });

                            hbs.RegisterHelper("divide", (writer, context, parameters) =>
                            {
                                try
                                {
                                    int a = int.Parse(parameters[0].ToString());
                                    int b = int.Parse(parameters[1].ToString());
                                    int c = a / b;
                                    writer.WriteSafeString(c.ToString());

                                }
                                catch (Exception)
                                {
                                    writer.WriteSafeString("0");
                                }
                            });

                            hbs.RegisterHelper("equal", (writer, options, context, arguments) =>
                            {
                                if (arguments.Length == 2 && arguments[0].Equals(arguments[1]))
                                {
                                    options.Template(writer, (object)context);
                                }
                                else
                                {
                                    options.Inverse(writer, (object)context);
                                }
                            });

                            hbs.RegisterHelper("script", (writer, options, context, arguments) =>
                            {
                                writer.WriteSafeString("<script>");
                                options.Template(writer, (object)context);
                                writer.WriteSafeString("</script>");
                            });

                            hbs.RegisterHelper("registerscript", (writer, context, parameters) =>
                            {
                                if (parameters.Length == 1)
                                {
                                    string jsfilename = Path.GetDirectoryName(RazorScriptFile).Replace("\\", "/") + "/" + parameters[0];
                                    ClientResourceManager.RegisterScript(Page, Page.ResolveUrl(jsfilename), FileOrder.Js.DefaultPriority);
                                    //writer.WriteSafeString(Page.ResolveUrl(jsfilename));
                                }
                            });
                            hbs.RegisterHelper("registerstylesheet", (writer, context, parameters) =>
                            {
                                if (parameters.Length == 1)
                                {
                                    string cssfilename = Path.GetDirectoryName(RazorScriptFile).Replace("\\", "/") + "/" + parameters[0];
                                    ClientResourceManager.RegisterStyleSheet(Page, Page.ResolveUrl(cssfilename), FileOrder.Css.PortalCss);
                                }
                            });

                            
                            var template = hbs.Compile(source);
                            var result = template(model);
                            //Controls.Add(new LiteralControl(Server.HtmlDecode(result)));
                            return result;
                        }
                    }
                    else
                    {
                        //Controls.Add(new LiteralControl(Server.HtmlDecode("No data found")));
                        return "No data found";
                    }

                    //JObject config = JObject.Parse(File.ReadAllText(filename));
                    /*
                    var converter = new ExpandoObjectConverter();
                    dynamic obj;
                    if (json is JArray)
                        obj = JsonConvert.DeserializeObject<List<ExpandoObject>>(data, converter);
                    else
                        obj = JsonConvert.DeserializeObject<ExpandoObject>(data, converter);

                     */

                }
                else
                {
                    return "No template found";
                }
            }
            catch (Exception ex)
            {
                Exceptions.ProcessModuleLoadException(this, ex);
                
            }
            return "";
        }
        /*
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
             
            }
        }
        */
        protected void cmdSave_Click(object sender, EventArgs e)
        {
            //ModuleController.Instance.UpdateModuleSetting(ModuleId, "field", txtField.Text);
            //DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, "Update Successful", DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
        }


        protected void cmdCancel_Click(object sender, EventArgs e)
        {
        }

        #endregion


        public DotNetNuke.Entities.Modules.Actions.ModuleActionCollection ModuleActions
        {
            get
            {
                var Actions = new ModuleActionCollection();
                Actions.Add(ModuleContext.GetNextActionID(),
                            Localization.GetString(ModuleActionType.AddContent, LocalResourceFile),
                            ModuleActionType.AddContent,
                            "",
                            "",
                            ModuleContext.EditUrl(),
                            false,
                            SecurityAccessLevel.Edit,
                            true,
                            false);

                Actions.Add(ModuleContext.GetNextActionID(),
                           Localization.GetString("EditTemplate.Action", LocalResourceFile),
                           ModuleActionType.ContentOptions,
                           "",
                           "",
                           ModuleContext.EditUrl("EditTemplate"),
                           false,
                           SecurityAccessLevel.Host,
                           true,
                           false);

                Actions.Add(ModuleContext.GetNextActionID(),
                           Localization.GetString("EditData.Action", LocalResourceFile),
                           ModuleActionType.EditContent,
                           "",
                           "",
                           ModuleContext.EditUrl("EditData"),
                           false,
                           SecurityAccessLevel.Host,
                           true,
                           false);

                Actions.Add(ModuleContext.GetNextActionID(),
                           Localization.GetString("ShareTemplate.Action", LocalResourceFile),
                           ModuleActionType.ContentOptions,
                           "",
                           "",
                           ModuleContext.EditUrl("ShareTemplate"),
                           false,
                           SecurityAccessLevel.Host,
                           true,
                           false);

                return Actions;
            }
        }

        public void RazorRender(WebPageBase Webpage, TextWriter writer, dynamic model)
        {
            
                var HttpContext = new HttpContextWrapper(System.Web.HttpContext.Current);

                if ((Webpage) is DotNetNukeWebPage<dynamic>)
                {
                    var mv = (DotNetNukeWebPage<dynamic>)Webpage;
                    mv.Model = model;
                }
                if (Webpage != null)
                    Webpage.ExecutePageHierarchy(new WebPageContext(HttpContext, Webpage, null), writer, Webpage);
                
        }

    }
}

