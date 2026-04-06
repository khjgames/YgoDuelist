Detailing Cardpack Visuals Creation 

All of these elements are bundled under a node in the scene tree, it has an aespect ratio of 600 x 846.

First we need to create the background for the cardpack.
@YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_Background.png

Portrait Template A:   248x197 aespect ratio,  includes a black and white Portrait_Mask where the black is masked and the white is shown, using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Portrait_Mask_A.png applied on a tags selected portrait and overlayed with @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Portrait_Outline_A.png 

Portrait Template B:   298x236 aespect ratio,  includes a black and white Portrait_Mask where the black is masked and the white is shown, using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Portrait_Mask_B.png applied on a tags selected portrait and overlayed with @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Portrait_Outline_B.png 

// start tag number specific visuals

    if this pack has exactly 1 tag: [

        We need to create a darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 1's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 

        We need to create a Portrait using the Portrait Template B, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/giant_soldier_of_stone.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (35, 29), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template B, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/slifer_the_sky_dragon.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (149, 304), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template B, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/card_destruction.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (260, 577), with its position relative to the original scale of the cardpack background (600x846)


        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_S_Tag_Darken_Tint.png 
    ]

    if this pack has exactly 2 tags: [

        We need to create a darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 1's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_D_Tag_1_Darken_Tint.png

        We need to create another darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 2's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_D_Tag_2_Darken_Tint.png 

        We need to create a Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/labyrinth_wall.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (6, 218), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template B, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/giant_soldier_of_stone.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (35, 29), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/the_earth_-_hex-sealed_fusion.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (315, 31), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/pot_of_greed.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (345, 424), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template B, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/card_destruction.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (260, 577), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/graceful_charity.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (28, 603), with its position relative to the original scale of the cardpack background (600x846)


        Next we need to create the Outline for the Cardpack, overlayed on top of the background.
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_D_Outline.png 
    ]

    if this pack has exactly 3 tags: [

        We need to create a darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 1's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Tag_1_Darken_Tint.png

        We need to create another darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 2's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Tag_2_Darken_Tint.png 

        Next, we need to create the Rift for the Cardpack, overlayed on top of the background.
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Rift.png 

        We need to create a Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/labyrinth_wall.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (6, 218), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template B, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/giant_soldier_of_stone.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (35, 29), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/the_earth_-_hex-sealed_fusion.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (315, 31), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/pot_of_greed.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (345, 424), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template B, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/card_destruction.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (260, 577), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template A, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/graceful_charity.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (28, 603), with its position relative to the original scale of the cardpack background (600x846)
     
        We need to create another Portrait using the Portrait Template C, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/graceful_charity.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (295, 225), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template D, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/card_destruction.png as the portrait. It should be scaled at 298x236 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (260, 577), with its position relative to the original scale of the cardpack background (600x846)

        We need to create another Portrait using the Portrait Template E, overlayed on top of the background. Using @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Tag_Portraits/pot_of_greed.png as the portrait. It should be scaled at 248x197 relative to the original scale of the cardpack background (600x846) and positioned with its top left corner at at (54, 424), with its position relative to the original scale of the cardpack background (600x846)

        Next we need to create the Outline for the Cardpack, overlayed on top of the background.
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Outline.png 
    ]

// end of tag number specific visuals



Lastly we need to create the Outline for the Cardpack, overlayed on top of the background.
@YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_Background_Outline.png