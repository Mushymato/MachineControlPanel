<frame left-click=|ToggleGlobalLocal()| focusable="true"
  background={@mushymato.MachineControlPanel/sprites/cursors:insetBg}
  padding="6">
  <globe *!if={IsGlobal} tint="#00000088" tooltip={:CurrentLocationName}/>
  <globe *if={IsGlobal} tint="White" tooltip={#machine-select.scope.everywhere}/>
</frame>

<template name="globe">
  <image 
    sprite={@mushymato.MachineControlPanel/sprites/furniture:globe}
    layout="48px 48px"
    tint={&tint}
    tooltip={&tooltip}
    +hover:scale="1.14"
  />
</template>
