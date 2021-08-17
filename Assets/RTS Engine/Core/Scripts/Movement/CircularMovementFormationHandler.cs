using UnityEngine;

using System.Collections.Generic;

namespace RTSEngine.Movement
{
    public class CircularMovementFormationHandler : BaseMovementFormationHandler
    {
        public override ErrorMessage GeneratePathDestinations (PathDestinationInputData input, ref int amount,
            ref float offset, ref List<Vector3> pathDestinations, out int generatedAmount)
        {
            // How many valid path destinations did this call generate?
            generatedAmount = 0; 

            float spacing = input.formationSelector.GetFloatPropertyValue(propName: "spacing");

            // Calculate the perimeter of the circle in which unoccupied positions will be searched
            // Then calculate the expected amount of free positions for the unit with unitRadius in the circle
            int expectedPositionCount = Mathf.FloorToInt(2.0f * Mathf.PI * offset / ((input.refMvtComp.Controller.Radius + spacing) * 2.0f));

            // If no expected positions are to be found and the radius offset is zero then set the expected position count to 1 to test the actual target position if it is valid
            if (expectedPositionCount == 0 && offset == 0.0f)
                expectedPositionCount = 1;

            // Represents increment value of the angle inside the current circle with the above perimeter
            float angleIncValue = 360f / expectedPositionCount;
            float currentAngle = 0.0f;

            // Get the initial path destination by picking the closest position on the circle around the target.
            Vector3 nextDestination = input.targetPosition + Vector3.right * offset;

            int counter = 0; 

            // As long as we haven't inspected all the expected free positions inside this cirlce
            while (counter < expectedPositionCount) 
            {
                ErrorMessage errorMessage = ErrorMessage.none;

                // Always make sure that the next path destination has a correct height in regards to the height of the map.
                nextDestination.y = terrainMgr.SampleHeight(nextDestination, input.refMvtComp);

                // Check if there is no obstacle and no other reserved target position on the currently computed potential path destination
                if((errorMessage = mvtMgr.IsPositionClear(ref nextDestination, input.refMvtComp, input.playerCommand)) == ErrorMessage.none
                    && (errorMessage = IsConditionFulfilled(input, nextDestination)) == ErrorMessage.none)
                {
                    amount--;
                    generatedAmount++;

                    // Save the valid destination.
                    pathDestinations.Add(nextDestination); 
                }
                // If while checking the target position, we left the bounds of the search grid then stop generating target positions immediately as we are no longer searching inside the map
                else if (errorMessage == ErrorMessage.searchCellNotFound)
                    return errorMessage;
                        
                // Increment the angle value to find the next position on the circle
                currentAngle += angleIncValue; 

                // Rotate the nextDestination vector around the y axis by the current angle value
                nextDestination = input.targetPosition + offset * new Vector3(Mathf.Cos(Mathf.Deg2Rad * currentAngle), 0.0f, Mathf.Sin(Mathf.Deg2Rad * currentAngle));

                counter++;
            }

            // Increase the circle radius by the unit's radius so we can calculate a destination position in a wider circle in the next iteration
            offset += input.refMvtComp.Controller.Radius + spacing;

            return ErrorMessage.none;
        }
    }
}
