// ------------------------------------------------------------------------------
//  _______   _____ ___ ___   _   ___ ___ 
// |_   _\ \ / / _ \ __/ __| /_\ | __| __|
//   | |  \ V /|  _/ _|\__ \/ _ \| _|| _| 
//   |_|   |_| |_| |___|___/_/ \_\_| |___|
// 
// This file has been generated automatically by TypeSafe.
// Any changes to this file may be lost when it is regenerated.
// https://www.stompyrobot.uk/tools/typesafe
// 
// TypeSafe Version: 1.5.0
// 
// ------------------------------------------------------------------------------



public sealed class SRSortingLayers {
    
    private SRSortingLayers() {
    }
    
    private const string _tsInternal = "1.5.0";
    
    public static global::TypeSafe.SortingLayer Back {
        get {
            return @__all[0];
        }
    }
    
    public static global::TypeSafe.SortingLayer Default {
        get {
            return @__all[1];
        }
    }
    
    public static global::TypeSafe.SortingLayer Front {
        get {
            return @__all[2];
        }
    }
    
    private static global::System.Collections.Generic.IList<global::TypeSafe.SortingLayer> @__all = new global::System.Collections.ObjectModel.ReadOnlyCollection<global::TypeSafe.SortingLayer>(new global::TypeSafe.SortingLayer[] {
                new global::TypeSafe.SortingLayer("Back", 1578769397),
                new global::TypeSafe.SortingLayer("Default", 0),
                new global::TypeSafe.SortingLayer("Front", -1376440981)});
    
    public static global::System.Collections.Generic.IList<global::TypeSafe.SortingLayer> All {
        get {
            return @__all;
        }
    }
}
