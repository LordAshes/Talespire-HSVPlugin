# HSV Plugin

This unofficial TaleSpire mod for changing the Hue, Saturation and Value of a mini from the GM menu.

## Change Log

```
2.0.0: Fix after BR HF Integration update
1.2.0: Uses queue to process HSV transformation to lower CPU load.
1.2.0: Processes all materials on a asset if it has multiple materials.
1.2.0: Simultaneous transformation use different texture files.
1.1.1: Added manual HSV trigger and option to turn off automatic triggers.
1.1.0: Added additional support for assets with non-readable textures.
1.0.1: Bug fix. Fixed paths to plugin and support utility.
1.0.0: Initial release
```

## Install

Use R2ModMan or similar installer to install.
Use R2ModMan to configure settings for the plugin.

## Usage

```
1. Click on a mini
2. From the GM Tools menu select the HSV option (paintbrush icon)
3. Adjust the Hue, Saturation and Value using the sliders.
4. Press Apply to view the results.
5. Press Exit when the desired values are applied.

Note: 100% for each value will result in the original texture. The button below the sliders can
      be used to set the values to 100%
```

RIGHT CTRL + V = Manually request application of HSV transforms

## Delay HSV Application On New Asset

When assets with HSV modifications are loaded during board load or added by the GM, the HSV values
will be restored. In some cases processing these transformations immediately can cause issues because
the asset is not yet fully ready. As a result, a setting in the configuration allows the introduction
of a delay between the asset being added and transformation applied.

### Delay HSV Application On Startup

The previous "Delay HSV Application On Startup" configuration has been removed. The above setting is
used for both board load and new assets added.

### Maximum Simultaneous Transformations

This new setting allows the configurations of the number of the transformations that can be processed
at the same time. The higher the number the faster the application of the HSV transformations but the
more CPU intensive the process. The lower the number, the longer it will take to process all HSV
transformations but it will take less CPU. Minimum 1.
