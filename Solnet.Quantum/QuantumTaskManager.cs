using Azure.Quantum.Jobs.Models;
using Microsoft.Azure.Quantum;

namespace Solnet.Quantum
{
    public class QuantumTaskManager
    {
        private static JobDetails CreateJobDetails(string jobId, string containerUri = null, string inputUri = null)
        {
            return new JobDetails(
                containerUri: containerUri,
                inputDataFormat: "microsoft.qio.v2",
                providerId: "Microsoft",
                target: "microsoft.paralleltempering-parameterfree.cpu")
            {
                Id = jobId,
                Name = "Azure.Quantum.Unittest",
                OutputDataFormat = "microsoft.qio-results.v2",
                InputParams = new Dictionary<string, object>()
                {
                   { "params", new Dictionary<string, object>() },
                },
                InputDataUri = inputUri,
            };
        }
        static async Task SubmitQsharpProgram(Workspace workspace, string jobName, string qsharpProgramPath, string containerUri)
        {
            var jobClient = workspace.Client;
            var job = CreateJobDetails(jobName, qsharpProgramPath, containerUri);

            await jobClient.CreateJobAsync(job.Name, job);
            Console.WriteLine($"Job submitted: {job.Name}");
            Console.WriteLine("Monitoring job status...");

            // Monitor job status
            var jobStatus = await jobClient.GetJobAsync(job.Id);
            while (jobStatus.Value.Status == "Running")
            {
                await Task.Delay(1000);
                jobStatus = await jobClient.GetJobAsync(job.Id);
                Console.WriteLine($"Job status: {jobStatus.Value.Status}");
            }

            if (jobStatus.Value.Status == "Succeeded")
            {
                Console.WriteLine("Job completed successfully!");
                // Fetch the results if required
                var resultsUri = jobStatus.Value.OutputDataUri;
                Console.WriteLine($"Results: {resultsUri}");
            }
            else
            {
                Console.WriteLine("Job failed.");
            }
        }
    }
}
