namespace Core
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Services.Common;

    /// <summary>
    /// Class which will deal with the throttled request. Starting on Feb 2017 1ES imposed a quota limit for the VSTS requests per identity.
    /// The limit is 10K calls per identity per rolling hour. In order for us to avoid having a sync mechanism which is shared by all pumps,
    /// we've decided to let the requests reach the limits and catch a VssException exception and sleep for a period of time before trying again.
    /// </summary>
    public class ThrottlingManager
    {
        /// <summary>
        /// The VssException exception could indicate our requests are being throttled
        /// If we get this exception we sleep for some time and then try again.
        /// If the request still fails, fail and bail out with the original exception.
        /// </summary>
        public static async Task<T> SleepAndRetry<T>(Func<Task<T>> action, int retryCount = 12, int sleepValueInSeconds = 300)
        {
            int sleepAttempts = 0;

            while (true)
            {
                try
                {
                    T result = await action();
                    return result;
                }
                catch (Exception ex)
                {
                    // Assume that it is a VssException first
                    VssException vssException = ex as VssException;

                    // If it's an aggregate exception instead
                    if (vssException == null && ex is AggregateException)
                    {
                        vssException = ((AggregateException)ex).Flatten().InnerExceptions.OfType<VssException>().FirstOrDefault();
                    }

                    // If still no VssException found then it's not what we are looking for, something else happened
                    if (vssException == null)
                    {
                        throw;
                    }

                    //// Now finally do the below work once a VssException has been found

                    if (!IsServiceUnavailableError(vssException) && !IsStandardThrottlingError(vssException) && !IsRateLimitError(vssException))
                    {
                        throw;
                    }

                    if (sleepAttempts >= retryCount)
                    {
                        Console.WriteLine($"Request still unsuccessful after {retryCount} retries.");
                        throw;
                    }

                    sleepAttempts++;

                    Console.WriteLine($"Attempt {sleepAttempts} of {retryCount}. {vssException.Message}. Sleeping for {TimeSpan.FromSeconds(sleepValueInSeconds).TotalMinutes} minutes");
                    await Task.Delay(TimeSpan.FromSeconds(sleepValueInSeconds));
                }
            }
        }

        private static bool IsStandardThrottlingError(Exception exc)
        {
            // Exception we should sleep on looks like: Microsoft.VisualStudio.Services.Common.VssServiceException:
            // TF400733: The request has been canceled: Request was blocked due to exceeding usage of resource 'TotalRequestsByServiceName' in namespace 'ServiceNameUser.'.
            // Returned ErrorCode is 0 for all exceptions so we can't use it to determine if we should sleep or not, instead we look for the 'TF*' code in the message.
            return exc.Message.Contains("TF400733");
        }

        private static bool IsRateLimitError(Exception exc)
        {
            // The error message should be something like this..
            // Request was blocked due to exceeding usage of resource 'TotalRequestsByServiceName' in namespace 'ServiceNameUser'.
            // For more information on why your request was blocked, see the topic "Rate limits"
            // Doesn't look like there is an errocode that we can check against, right now it's always 0 so we have to check against the message
            // Though, it does have an Hresult value of -2146232832 but unsure if this is unique to this error or not
            return exc.Message.Contains("Request was blocked") && exc.Message.Contains("Rate limits");
        }

        private static bool IsServiceUnavailableError(Exception exc)
        {
            // Exception we should sleep on looks like: Microsoft.VisualStudio.Services.WebApi.VssServiceResponseException: Service Unavailable
            if (exc.Message.Contains("Service Unavailable"))
            {
                return true;
            }

            // Exception looks like this: VssServiceException: TF401409: There was a generic database update error. Please try again.
            else if (exc.Message.Contains("TF401409"))
            {
                return true;
            }

            // Exception looks like this: CircuitBreakerExceededConcurrencyException: TF400898: An Internal Error Occurred.
            else if (exc.Message.Contains("TF400898"))
            {
                return true;
            }

            return false;
        }
    }
}
