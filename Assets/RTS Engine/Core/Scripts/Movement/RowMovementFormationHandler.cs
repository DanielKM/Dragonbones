using System.Collections.Generic;

using UnityEngine;

namespace RTSEngine.Movement
{
    public class RowMovementFormationHandler : BaseMovementFormationHandler
    {
        public override ErrorMessage GeneratePathDestinations(PathDestinationInputData input, ref int amount,
            ref float offset, ref List<Vector3> pathDestinations, out int generatedAmount)
        {
            // How many valid path destinations did this call generate?
            generatedAmount = 0;

            float horizontalSpacing = input.formationSelector.GetFloatPropertyValue(propName: "horizontal-spacing", fallbackPropName: "spacing");

            float verticalSpacing = input.formationSelector.GetFloatPropertyValue(propName: "vertical-spacing", fallbackPropName: "spacing");

            int amountPerRow = input.formationSelector.GetIntPropertyValue(propName: "amountPerRow");

            // The row direction is simply the direction vector but with inverted x and z coordinates with negating the resulting z axis.
            // The y coordinate is set to 0 since we will rely on the terrain manager to generate the height of potential path destination each time.
            Vector3 rowDirection = new Vector3(input.direction.z, 0.0f, -input.direction.x).normalized;

            // Before starting to compute the potential path destinations, we move the target position in the row directoin by the current offset and we start generating destinations at that point.
            Vector3 offsetTargetPosition = input.targetPosition - input.direction * offset;

            // Allows to alternate between going left and right while generating path destinations following one row
            int multiplier = 1; 

            int counter = 0; 

            // As long as there path destinations to produce and we still haven't got over the allowed amount of units per row
            while (counter < amountPerRow)
            {
                ErrorMessage errorMessage = ErrorMessage.none;

                // Each time, we once go right in the rowDirection and then we go left in the rowDirection using the same distance from the target position.
                Vector3 nextDestination = offsetTargetPosition
                    + multiplier * (input.refMvtComp.Controller.Radius + horizontalSpacing) * rowDirection;

                // Always make sure that the next path destination has a correct height in regards to the height of the map.
                nextDestination.y = terrainMgr.SampleHeight(nextDestination, input.refMvtComp);

                // Check if there is no obstacle and no other reserved target position on the currently computed potential path destination
                if((errorMessage = mvtMgr.IsPositionClear(ref nextDestination, input.refMvtComp, input.playerCommand)) == ErrorMessage.none
                    && (errorMessage = IsConditionFulfilled(input, nextDestination)) == ErrorMessage.none)
                {
                    amount--;
                    generatedAmount++;

                    pathDestinations.Add(nextDestination); // Save the valid destination.
                }
                // If while checking the target position, we left the bounds of the search grid then stop generating target positions immediately as we are no longer searching inside the map
                else if (errorMessage == ErrorMessage.searchCellNotFound)
                    return errorMessage;

                multiplier = -multiplier + (multiplier < 0 ? 2 : 0); // Allows us to move right then left by the same distance each two iterations.

                counter++;
            }

            // For the next method call, this allows us to move one row back.
            offset += input.refMvtComp.Controller.Radius + verticalSpacing;

            return ErrorMessage.none;
        }
    }
}
