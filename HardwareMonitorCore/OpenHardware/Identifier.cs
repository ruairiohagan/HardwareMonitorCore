﻿/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2010 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

namespace OpenHardwareMonitor.Hardware {
  public class Identifier : IComparable<Identifier> {
    private readonly string identifier;

    private const char Separator = '/';

    private static void CheckIdentifiers(IEnumerable<string> identifiers) {      
      foreach (string s in identifiers)
        if (s.Contains(" ") || s.Contains(Separator.ToString()))
          throw new ArgumentException("Invalid identifier");
    }

    public Identifier(params string[] identifiers) {
      CheckIdentifiers(identifiers);

      StringBuilder s = new StringBuilder();
      for (int i = 0; i < identifiers.Length; i++) {
        s.Append(Separator);
        s.Append(identifiers[i]);
      }
      this.identifier = s.ToString();
    }

    public Identifier(Identifier identifier, params string[] extensions) {
      CheckIdentifiers(extensions);

      StringBuilder s = new StringBuilder();
      s.Append(identifier.ToString());
      for (int i = 0; i < extensions.Length; i++) {
        s.Append(Separator);
        s.Append(extensions[i]);
      }
      this.identifier = s.ToString();
    }

    public override string ToString() {
      return identifier;
    }

        public override bool Equals(Object obj) {
      if (obj == null)
        return false;

      Identifier id = obj as Identifier;
            if (id == null)
        return false;

            return (identifier == id.identifier);
    }

        public override int GetHashCode() {
      return identifier.GetHashCode();
    }

        public int CompareTo(Identifier other) {
      if (other == null)
        return 1;
      else 
        return string.Compare(this.identifier, other.identifier, 
          StringComparison.Ordinal);
    }

        public static bool operator ==(Identifier id1, Identifier id2) {
      if (id1.Equals(null))
        return id2.Equals(null);
      else
        return id1.Equals(id2);
    }

    public static bool operator !=(Identifier id1, Identifier id2) {
      return !(id1 == id2);
    }

    public static bool operator <(Identifier id1, Identifier id2) {
      if (id1 == null)
        return id2 != null;
      else 
        return (id1.CompareTo(id2) < 0);
    }

    public static bool operator >(Identifier id1, Identifier id2) {
      if (id1 == null)
        return false;
      else 
        return (id1.CompareTo(id2) > 0);
    }  

  }
}
