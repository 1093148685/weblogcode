namespace Weblog.Core.Common.Result;

public class Result
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = "success";
    public bool Success { get; set; } = true;

    public static Result Ok(string message = "success")
    {
        return new Result
        {
            Code = 200,
            Message = message,
            Success = true
        };
    }

    public static Result Fail(string message = "fail", int code = 500)
    {
        return new Result
        {
            Code = code,
            Message = message,
            Success = false
        };
    }
}

public class Result<T>
{
    public int Code { get; set; } = 200;
    public string Message { get; set; } = "success";
    public bool Success { get; set; } = true;
    public T? Data { get; set; }

    public static Result<T> Ok(T data, string message = "success")
    {
        return new Result<T>
        {
            Code = 200,
            Message = message,
            Success = true,
            Data = data
        };
    }

    public static Result<T> Fail(string message = "fail", int code = 500)
    {
        return new Result<T>
        {
            Code = code,
            Message = message,
            Success = false,
            Data = default
        };
    }
}
