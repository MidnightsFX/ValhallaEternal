using Jotunn.Entities;

namespace ValhallEternal.common
{
    /// <summary>
    /// A console command gated on the world's admin list rather than on <c>devcommands</c>. A solo
    /// player or host always passes; a client passes only when its user id is on the server's
    /// adminlist.txt, which the server syncs to every client, so both sides read the same list.
    /// </summary>
    /// <remarks>
    /// The check has to live in the command body. Jotunn maps <see cref="ConsoleCommand.IsCheat"/> onto
    /// the vanilla <c>isCheat</c> flag - that is the <c>devcommands</c> gate, not an admin gate - and it
    /// always passes <c>onlyAdmin: false</c>. Vanilla's <c>onlyAdmin</c> would not help anyway: nothing
    /// in the game reads <c>Terminal.ConsoleCommand.OnlyAdmin</c>, and only the
    /// <c>ConsoleEventFailable</c> overload folds it into <c>OnlyServer</c> - the <c>ConsoleEvent</c>
    /// overload Jotunn constructs drops it.
    /// </remarks>
    internal abstract class AdminConsoleCommand : ConsoleCommand
    {
        /// <summary>The command body, reached only once the caller has been confirmed as an admin.</summary>
        protected abstract void Execute(string[] args);

        // Sealed so no subclass can reintroduce an ungated entry point: Jotunn's Run(args, context)
        // funnels into this one, so gating here covers both. Evaluated per invocation rather than at
        // registration, when there is no ZNet yet.
        public sealed override void Run(string[] args)
        {
            if (ZNet.instance == null || !ZNet.instance.LocalPlayerIsAdminOrHost())
            {
                string message = $"'{Name}' requires admin.";
                Logger.LogInfo(message);
                if (Console.instance != null) { Console.instance.Print(message); }
                return;
            }

            Execute(args);
        }
    }
}
