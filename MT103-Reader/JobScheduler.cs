using Quartz;
using Quartz.Impl;
using System;
using System.Diagnostics;

namespace MT103_Reader {

    ///subclass of IJob for specifying work to be done
    class JobScheduler : IJob {

        public void Execute(IJobExecutionContext context) {
            try {

                /// Run the service
                var SwiftReaderService = new SwiftReaderService();
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

    }

    public class SwiftReaderServiceJobScheduler
    {
        /// <summary>
        /// Starts the scheduler job on firts run
        /// </summary>
        public static void Start()
        {
            //instantioate the schduler
            IScheduler scheduler            = StdSchedulerFactory.GetDefaultScheduler();
            
            //Set the global trigger            
            //Fire every 10 minutes between 8PM and 8PM every Weekday
            ITrigger trigger        = TriggerBuilder.Create().WithCronSchedule("10 * * * * ?", x => x.WithMisfireHandlingInstructionDoNothing()).Build();
            IJobDetail jobDetail    = JobBuilder.Create<JobScheduler>().Build();

            //schedule the job
            scheduler.ScheduleJob(jobDetail, trigger);

            //start the scheduler
            scheduler.Start();
        }
    }
}
