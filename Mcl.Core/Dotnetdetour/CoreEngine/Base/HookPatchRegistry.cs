using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Base;

/// <summary>Prevents re-patching a trampoline during staged discovery, reentry or assembly-load callbacks.</summary>
internal sealed class HookPatchRegistry
{
    private enum State
    {
        Installing,
        Installed,
        Failed
    }

    private readonly object _sync = new object();

    private readonly Dictionary<Tuple<MethodBase, MethodBase>, State> _states =
        new Dictionary<Tuple<MethodBase, MethodBase>, State>();

    internal bool InstallOnce(MethodBase target, MethodBase hook, Action install)
    {
        var key = Tuple.Create(target, hook);
        lock (_sync)
        {
            if (_states.TryGetValue(key, out var state))
            {
                if (state == State.Failed)
                    throw new InvalidOperationException(
                        "A previous native patch failed; restart the process before retrying: " + hook);
                return false;
            }

            _states[key] = State.Installing;
            try
            {
                install();
                _states[key] = State.Installed;
                return true;
            }
            catch
            {
                // A failed native write may already be partial. Never blindly patch it a second time.
                _states[key] = State.Failed;
                throw;
            }
        }
    }
}