/*
 
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at http://mozilla.org/MPL/2.0/.
 
  Copyright (C) 2009-2011 Michael Möller <mmoeller@openhardwaremonitor.org>
	
*/

using System;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.

namespace OpenHardwareMonitor.Hardware {
  internal abstract class Hardware : IHardware {

    private readonly Identifier identifier;
    protected readonly string name;
    private string customName;

        public Hardware(string name, Identifier identifier) {
      this.identifier = identifier;
      this.name = name;
    }

        public IHardware[] SubHardware {
      get { return new IHardware[0]; }
    }

    public virtual IHardware Parent {
            get { return null; }
        }

    public string Name {
      get {
        return customName;
      }
      set {
        if (!string.IsNullOrEmpty(value))
          customName = value;
        else
          customName = name;
      }
    }

    public Identifier Identifier {
      get {
        return identifier;
      }
    }

    public abstract HardwareType HardwareType { get; }

    public virtual string GetReport() {
      return null;
    }

    public abstract void Update();

  }
}
