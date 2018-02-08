namespace Flame
{
    /// <summary>
    /// Specifies the exception throwing behavior of a method or instruction.
    /// </summary>
    public abstract class ExceptionSpecification
    {
        /// <summary>
        /// Tells if this exception specification allows for any exceptions
        /// at all to be thrown.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this exception specification allows for
        /// at least one type of exception to be thrown; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanThrowSomething { get; }

        /// <summary>
        /// Tells if this exception specification allows for an exception
        /// of a particular type or a derived type to be thrown.
        /// </summary>
        /// <param name="exceptionType">The type of exception to examine.</param>
        /// <returns>
        /// <c>true</c> if an exception of type <paramref name="exceptionType"/>
        /// or a derived type can be thrown; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanThrow(IType exceptionType);

        /// <summary>
        /// An exception specification that does not allow for any exceptions
        /// to be thrown.
        /// </summary>
        /// <returns>A no-throw exception specification.</returns>
        public static readonly ExceptionSpecification NoThrow
            = new NoThrowExceptionSpecification();

        /// <summary>
        /// An exception specification that allows for an exception of any type
        /// to be thrown.
        /// </summary>
        /// <returns>A throw-any exception specification.</returns>
        public static readonly ExceptionSpecification ThrowAny
            = new ThrowAnyExceptionSpecification();
    }

    internal sealed class NoThrowExceptionSpecification : ExceptionSpecification
    {
        public override bool CanThrowSomething => false;

        public override bool CanThrow(IType exceptionType)
        {
            return false;
        }
    }

    internal sealed class ThrowAnyExceptionSpecification : ExceptionSpecification
    {
        public override bool CanThrowSomething => true;

        public override bool CanThrow(IType exceptionType)
        {
            return true;
        }
    }
}