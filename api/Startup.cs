using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Models;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SQLite;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(api.Startup))]

namespace api
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // 有关如何配置应用程序的详细信息，请访问 https://go.microsoft.com/fwlink/?LinkID=316888

            // Hangfire configuration
            Hangfire.GlobalConfiguration.Configuration.UseSQLiteStorage("HangfireDB", new SQLiteStorageOptions());

            //启用服务
            app.UseHangfireServer(new BackgroundJobServerOptions
            {
                Queues = new string[] { "default" },
                WorkerCount = 1
            });

            List<HangfireUserID> users = new List<HangfireUserID>();

            try
            {
                string user = System.Configuration.ConfigurationManager.AppSettings["Hangfire_User"] ?? "[]";
                users = JsonHelper.Deserialize<List<HangfireUserID>>(user);
            }
            catch (Exception exp)
            {

            }

            //Hangfire 仪表盘用户验证
            if (users != null && users.Count > 0)
            {
                List<BasicAuthAuthorizationUser> _userList = new List<BasicAuthAuthorizationUser>();
                foreach (HangfireUserID uid in users)
                {
                    _userList.Add(new BasicAuthAuthorizationUser
                    {
                        Login = uid.user_id,
                        // Password as plain text
                        PasswordClear = uid.user_pwd
                    });
                }



                //启用Dashboard面板
                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    Authorization = new BasicAuthAuthorizationFilter[]
                    {
                        new BasicAuthAuthorizationFilter ( new BasicAuthAuthorizationFilterOptions
                        {
                            SslRedirect = false,
                            RequireSsl = false,
                            LoginCaseSensitive = true,
                            Users = _userList
                        } )
                    }
                });

            }

            //无用户验证
            else
            {
                //启用Dashboard面板
                app.UseHangfireDashboard("/hangfire");
            }


            //

            // Let's also create a sample background job
            BackgroundJob.Enqueue(() => System.Diagnostics.Debug.WriteLine("Hello world from Hangfire!"));

            // Add scheduled jobs
            RecurringJob.AddOrUpdate("1", () => Run(), GetConfigCron("AutoCreate_Cron"), TimeZoneInfo.Local);

            //RecurringJob.AddOrUpdate("2", () => Run2(), "0 */1 * * * ?", TimeZoneInfo.Local, "q1");





        }

        /// <summary>
        /// Cron表达式
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns></returns>
        public static string GetConfigCron(string key)
        {
            try
            {
                string _Cron = System.Configuration.ConfigurationManager.AppSettings[key] ?? "";
                return string.IsNullOrEmpty(_Cron) ? null : _Cron;
            }
            catch
            {
                return null;
            }
        }


        public static void Run()
        {
            string id = DateTime.Now.Ticks + "";
            var jobId = BackgroundJob.Enqueue(() => Run_1(" " + id + "1"));
            
            jobId = BackgroundJob.ContinueJobWith(jobId, () => Run_1(" " + id + "2"));
            
            jobId = BackgroundJob.ContinueJobWith(jobId, () => Run_1(" " + id + "3"));
            
            System.Threading.Thread.Sleep(1000);
        }

        public static void Run_1(string num)
        {
            System.Threading.Thread.Sleep(500);
            System.Diagnostics.Debug.WriteLine("Run "+ num+" ...");
        }

    }
}
