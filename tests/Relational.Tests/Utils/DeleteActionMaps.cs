using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum DeleteAction
{
    NoAction,
    Cascade,
    SetNull,
    SetDefault,
    Restrict // included for cross-provider normalization if you want
}

public static class DeleteActionMaps
{
    public static DeleteAction FromSqlServer(string desc)
        => desc?.ToUpperInvariant() switch
        {
            "CASCADE" => DeleteAction.Cascade,
            "SET_NULL" => DeleteAction.SetNull,
            "SET_DEFAULT" => DeleteAction.SetDefault,
            "NO_ACTION" => DeleteAction.NoAction,
            _ => DeleteAction.NoAction
        };

    public static DeleteAction FromEf(DeleteBehavior b)
        => b switch
        {
            DeleteBehavior.Cascade => DeleteAction.Cascade,
            DeleteBehavior.SetNull => DeleteAction.SetNull,
            DeleteBehavior.ClientSetNull => DeleteAction.NoAction, // SQL Server ends up NO ACTION/RESTRICT-ish
            DeleteBehavior.Restrict => DeleteAction.Restrict,
            DeleteBehavior.NoAction => DeleteAction.NoAction,
            _ => DeleteAction.NoAction
        };
}