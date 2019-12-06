using System;

namespace QueueProcessor
{
    public struct Result : IEquatable<Result>
    {
        private Result(bool isError, string? code, Exception? exception)
        {
            if (!isError && exception != null)
            {
                throw new ArgumentException("Successful response cannot have an exception.", nameof(exception));
            }

            this.IsError = isError;
            this.Code = code;
            this.Exception = exception;
        }

        public bool IsError { get; }

        public string? Code { get; }

        public Exception? Exception { get; }

        public static Result Ok() => new Result(false, null, null);
        public static Result Ok(string? code) => new Result(false, code, null);
        public static Result Error(string? code) => new Result(true, code, null);
        public static Result Error(Exception? exception) => new Result(true, null, exception);
        public static Result Error(string? code, Exception? exception) => new Result(true, code, exception);

        public override bool Equals(object obj) => obj is Result && Equals((Result)obj);
        public bool Equals(Result other) => this.IsError == other.IsError && string.Equals(this.Code, other.Code, StringComparison.OrdinalIgnoreCase) && Equals(this.Exception, other.Exception);
        public override int GetHashCode() => HashCode.Combine(this.IsError, this.Code, this.Exception);
        public static bool operator ==(Result left, Result right) => left.Equals(right);
        public static bool operator !=(Result left, Result right) => !(left == right);

        public override string ToString() => this.IsError
            ? (this.Code == null ? "Failed" : "Failed(" + this.Code + ")")
            : (this.Code == null ? "Done" : "Done(" + this.Code + ")");
    }
}
