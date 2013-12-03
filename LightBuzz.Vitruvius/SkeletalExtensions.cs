﻿using System;
using Microsoft.Kinect;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace LightBuzz.Vitruvius
{
    /// <summary>
    /// Provides some common functionality for manupulating skeletal data.
    /// </summary>
    public static class SkeletalExtensions
    {
        #region Public methods

        /// <summary>
        /// Retruns the height of the specified skeleton.
        /// </summary>
        /// <param name="skeleton">The specified user skeleton.</param>
        /// <returns>The height of the skeleton in meters.</returns>
        public static double Height(this Skeleton skeleton)
        {
            const double HEAD_DIVERGENCE = 0.1;

            var head = skeleton.Joints[JointType.Head];
            var neck = skeleton.Joints[JointType.ShoulderCenter];
            var spine = skeleton.Joints[JointType.Spine];
            var waist = skeleton.Joints[JointType.HipCenter];
            var hipLeft = skeleton.Joints[JointType.HipLeft];
            var hipRight = skeleton.Joints[JointType.HipRight];
            var kneeLeft = skeleton.Joints[JointType.KneeLeft];
            var kneeRight = skeleton.Joints[JointType.KneeRight];
            var ankleLeft = skeleton.Joints[JointType.AnkleLeft];
            var ankleRight = skeleton.Joints[JointType.AnkleRight];
            var footLeft = skeleton.Joints[JointType.FootLeft];
            var footRight = skeleton.Joints[JointType.FootRight];

            // Find which leg is tracked more accurately.
            int legLeftTrackedJoints = NumberOfTrackedJoints(hipLeft, kneeLeft, ankleLeft, footLeft);
            int legRightTrackedJoints = NumberOfTrackedJoints(hipRight, kneeRight, ankleRight, footRight);

            double legLength = legLeftTrackedJoints > legRightTrackedJoints ? Distance(hipLeft, kneeLeft, ankleLeft, footLeft) : Distance(hipRight, kneeRight, ankleRight, footRight);            

            return Distance(head, neck, spine, waist) + legLength + HEAD_DIVERGENCE;
        }

        /// <summary>
        /// Returns the upper height of the specified skeleton (head to waist). Useful whenever Kinect provides a way to track seated users.
        /// </summary>
        /// <param name="skeleton">The specified user skeleton.</param>
        /// <returns>The upper height of the skeleton in meters.</returns>
        public static double UpperHeight(this Skeleton skeleton)
        {
            var head = skeleton.Joints[JointType.Head];
            var neck = skeleton.Joints[JointType.ShoulderCenter];
            var spine = skeleton.Joints[JointType.Spine];
            var waist = skeleton.Joints[JointType.HipCenter];

            return Distance(head, neck, spine, waist);
        }

        /// <summary>
        /// Returns the length of the segment defined by the specified joints.
        /// </summary>
        /// <param name="p1">The first joint (start of the segment).</param>
        /// <param name="p2">The second joint (end of the segment).</param>
        /// <returns>The length of the segment in meters.</returns>
        public static double Distance(Joint p1, Joint p2)
        {
            return Math.Sqrt(
                Math.Pow(p1.Position.X - p2.Position.X, 2) +
                Math.Pow(p1.Position.Y - p2.Position.Y, 2) +
                Math.Pow(p1.Position.Z - p2.Position.Z, 2));
        }

        /// <summary>
        /// Returns the length of the segments defined by the specified joints.
        /// </summary>
        /// <param name="joints">A collection of two or more joints.</param>
        /// <returns>The length of all the segments in meters.</returns>
        public static double Distance(params Joint[] joints)
        {
            double length = 0;

            for (int index = 0; index < joints.Length - 1; index++)
            {
                length += Distance(joints[index], joints[index + 1]);
            }

            return length;
        }

        /// <summary>
        /// Returns the distance of the specified joints.
        /// </summary>
        /// <param name="p1">The first joint (start of the segment).</param>
        /// <param name="p2">The second joint (end of the segment).</param>
        /// <returns>The length of the segment in meters.</returns>
        public static double DistanceFrom(this Joint p1, Joint p2)
        {
            return Distance(p1, p2);
        }

        /// <summary>
        /// Given a collection of joints, calculates the number of the joints that are tracked accurately.
        /// </summary>
        /// <param name="joints">A collection of joints.</param>
        /// <returns>The number of the accurately tracked joints.</returns>
        public static int NumberOfTrackedJoints(params Joint[] joints)
        {
            int trackedJoints = 0;

            foreach (var joint in joints)
            {
                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    trackedJoints++;
                }
            }

            return trackedJoints;
        }

        /// <summary>
        /// Scales the specified joint according to the specified dimensions.
        /// </summary>
        /// <param name="joint">The joint to scale.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="skeletonMaxX">Maximum X.</param>
        /// <param name="skeletonMaxY">Maximum Y.</param>
        /// <returns>The scaled version of the joint.</returns>
        public static Joint ScaleTo(this Joint joint, int width, int height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new SkeletonPoint()
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        /// <summary>
        /// Scales the specified joint according to the specified dimensions.
        /// </summary>
        /// <param name="joint">The joint to scale.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <returns>The scaled version of the joint.</returns>
        public static Joint ScaleTo(this Joint joint, int width, int height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        public static object SerializeJoint(this Skeleton skeleton, JointType joint)
        {
            Vector4 q = skeleton.BoneOrientations[joint].HierarchicalRotation.Quaternion;
            return new
            {
                X = skeleton.Joints[joint].Position.X,
                Y = skeleton.Joints[joint].Position.Y,
                Z = skeleton.Joints[joint].Position.Z,
                state = skeleton.Joints[joint].TrackingState.ToString(),
                rotationFrom = skeleton.BoneOrientations[joint].StartJoint.ToString(),
                rotationQuaternion = new[] {q.X, q.Y, q.Z, q.W}
            };
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the scaled value of the specified position.
        /// </summary>
        /// <param name="maxPixel">Width or height.</param>
        /// <param name="maxSkeleton">Border (X or Y).</param>
        /// <param name="position">Original position (X or Y).</param>
        /// <returns>The scaled value of the specified position.</returns>
        private static float Scale(int maxPixel, float maxSkeleton, float position)
        {
            float value = ((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));
            
            if (value > maxPixel)
            {
                return maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        #endregion
    }
}