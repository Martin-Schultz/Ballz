﻿//
//  Camera.cs
//
//  Author:
//       Martin <Martin.Schultz@RWTH-Aachen.de>
//
//  Copyright (c) 2015 SPAG
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using Microsoft.Xna.Framework;

namespace Ballz
{
    //TODO: implement the camera properly
    public class Camera
    {
        public Matrix View{ get; private set;}

        public Matrix Projection{ get; private set;}

        public Camera()
        {
            View = new Matrix();
            Projection = new Matrix();
        }

        public void SetView( Matrix view)
        {
            View = view;
        }

        public void SetProjection(Matrix projection)
        {
            Projection = projection;
        }
    }
}
