using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;

namespace EventBusWithQuartz.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuartzDashboardController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public QuartzDashboardController(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        [HttpGet("scheduler-info")]
        public async Task<IActionResult> GetSchedulerInfo()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            var info = new
            {
                SchedulerName = scheduler.SchedulerName,
                SchedulerInstanceId = scheduler.SchedulerInstanceId,
                IsStarted = scheduler.IsStarted,
                IsShutdown = scheduler.IsShutdown,
                InStandbyMode = scheduler.InStandbyMode,
                Version = "3.15.0" // Static version for now
            };

            return Ok(info);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            var executingJobs = await scheduler.GetCurrentlyExecutingJobs();
            
            // Count triggers by state
            var triggerStates = new Dictionary<string, int>();
            foreach (var triggerKey in triggerKeys)
            {
                var state = await scheduler.GetTriggerState(triggerKey);
                var stateName = state.ToString();
                triggerStates[stateName] = triggerStates.ContainsKey(stateName) ? triggerStates[stateName] + 1 : 1;
            }

            var stats = new
            {
                TotalJobs = jobKeys.Count,
                TotalTriggers = triggerKeys.Count,
                CurrentlyExecuting = executingJobs.Count,
                TriggerStates = triggerStates,
                Timestamp = DateTime.UtcNow
            };

            return Ok(stats);
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            
            var jobs = new List<object>();

            foreach (var jobKey in jobKeys)
            {
                var jobDetail = await scheduler.GetJobDetail(jobKey);
                var triggers = await scheduler.GetTriggersOfJob(jobKey);
                
                var job = new
                {
                    JobKey = jobKey.ToString(),
                    JobType = jobDetail?.JobType.Name,
                    Description = jobDetail?.Description,
                    Durable = jobDetail?.Durable,
                    PersistJobDataAfterExecution = jobDetail?.PersistJobDataAfterExecution,
                    ConcurrentExecutionDisallowed = jobDetail?.ConcurrentExecutionDisallowed,
                    RequestsRecovery = jobDetail?.RequestsRecovery,
                    Triggers = triggers.Select(t => new
                    {
                        Key = t.Key.ToString(),
                        Description = t.Description,
                        State = scheduler.GetTriggerState(t.Key).Result.ToString(),
                        NextFireTime = t.GetNextFireTimeUtc()?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        PreviousFireTime = t.GetPreviousFireTimeUtc()?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        StartTime = t.StartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        EndTime = t.EndTimeUtc?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        Priority = t.Priority,
                        MisfireInstruction = t.MisfireInstruction,
                        CalendarName = t.CalendarName
                    }).ToArray()
                };

                jobs.Add(job);
            }

            return Ok(jobs);
        }

        [HttpGet("triggers")]
        public async Task<IActionResult> GetTriggers()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            
            var triggers = new List<object>();

            foreach (var triggerKey in triggerKeys)
            {
                var trigger = await scheduler.GetTrigger(triggerKey);
                var state = await scheduler.GetTriggerState(triggerKey);

                try
                {
                    triggers.Add(new
                    {
                        Key = triggerKey.ToString(),
                        JobKey = trigger.JobKey.ToString(),
                        Description = trigger.Description,
                        State = state.ToString(),
                        NextFireTime = trigger.GetNextFireTimeUtc()?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        StartTime = trigger.StartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        EndTime = trigger.EndTimeUtc?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        Priority = trigger.Priority,
                        MisfireInstruction = trigger.MisfireInstruction
                    });
                }
                catch (Exception ex)
                {
                }
            }

            return Ok(triggers);
        }

        [HttpGet("executing-jobs")]
        public async Task<IActionResult> GetExecutingJobs()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var executingJobs = await scheduler.GetCurrentlyExecutingJobs();

            var jobs = executingJobs.Select(job => new
            {
                JobKey = job.JobDetail.Key.ToString(),
                TriggerKey = job.Trigger.Key.ToString(),
                FireTime = job.FireTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ScheduledFireTime = job.ScheduledFireTimeUtc?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                RunTime = job.JobRunTime.TotalMilliseconds,
                Recovering = job.Recovering
            });

            return Ok(jobs);
        }

        [HttpPost("pause-job/{group}/{name}")]
        public async Task<IActionResult> PauseJob(string group, string name)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey(name, group);
                
                if (!await scheduler.CheckExists(jobKey))
                {
                    return NotFound($"Job {jobKey} not found");
                }
                
                await scheduler.PauseJob(jobKey);
                
                return Ok($"Job {jobKey} paused successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error pausing job: {ex.Message}");
            }
        }

        [HttpPost("resume-job/{group}/{name}")]
        public async Task<IActionResult> ResumeJob(string group, string name)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey(name, group);
                
                if (!await scheduler.CheckExists(jobKey))
                {
                    return NotFound($"Job {jobKey} not found");
                }
                
                await scheduler.ResumeJob(jobKey);
                
                return Ok($"Job {jobKey} resumed successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error resuming job: {ex.Message}");
            }
        }

        [HttpPost("trigger-job/{group}/{name}")]
        public async Task<IActionResult> TriggerJob(string group, string name)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey(name, group);
                
                if (!await scheduler.CheckExists(jobKey))
                {
                    return NotFound($"Job {jobKey} not found");
                }
                
                await scheduler.TriggerJob(jobKey);
                
                return Ok($"Job {jobKey} triggered successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error triggering job: {ex.Message}");
            }
        }

        [HttpDelete("delete-job/{group}/{name}")]
        public async Task<IActionResult> DeleteJob(string group, string name)
        {
            try
            {
                var scheduler = await _schedulerFactory.GetScheduler();
                var jobKey = new JobKey(name, group);
                
                if (!await scheduler.CheckExists(jobKey))
                {
                    return NotFound($"Job {jobKey} not found");
                }
                
                var deleted = await scheduler.DeleteJob(jobKey);
                
                return Ok(new { JobKey = jobKey.ToString(), Deleted = deleted });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting job: {ex.Message}");
            }
        }

        [HttpGet("job-history")]
        public async Task<IActionResult> GetJobHistory()
        {
            // Note: This would require additional implementation to track job execution history
            // You might want to implement custom job listeners to store execution history
            return Ok("Job history tracking would require custom implementation with job listeners");
        }
    }
}