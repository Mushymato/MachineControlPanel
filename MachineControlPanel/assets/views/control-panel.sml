<lane orientation="vertical" horizontal-content-alignment="end" button-press=|HandlePagingButton($Button)|>
  <lane orientation="horizontal" padding="0,46,26,-16" vertical-content-alignment="end">
    <frame *if={:IsMainPlayer} background={@mushymato.MachineControlPanel/sprites/cursors:insetBg} layout="60px 60px">
      <checkbox is-checked={<>ToggleAll} tooltip={ToggleAllTooltip} margin="12"/>
    </frame>
    <include name="mushymato.MachineControlPanel/views/includes/global-toggle" *context={:GlobalToggle}/>
    <textinput text={<>SearchText} placeholder={#rule-list.search} background={@mushymato.MachineControlPanel/sprites/cursors:insetBg} layout="240px 60px" text-color="#43111B" focusable="true"/>
  </lane>
  <frame layout="1244px 90%[580..]"
    background={@Mods/StardewUI/Sprites/MenuBackground}
    border={@Mods/StardewUI/Sprites/MenuBorder}
    border-thickness="32, 36, 24, 36"
    *switch={PageIndex} horizontal-content-alignment="middle">
    <lane *float="above" orientation="horizontal" vertical-content-alignment="end" margin="36,0,0,-24">
      <tab-label page="1" text={#rule-list.rules} margin={TabMarginRules} />
      <tab-label *if={HasInputs} page="2" text={#rule-list.inputs} margin={TabMarginInputs}/>
      <image sprite={:MachineData}
        tooltip={:MachineTooltip}
        layout="48px 96px"
        margin="12,-16,12,0"
        fit="Contain"
        horizontal-alignment="middle"
        vertical-alignment="middle"
      />
      <label text={:MachineName} tooltip={:MachineTooltip} font="dialogue" color="white" margin="0,24" />
    </lane>

    <!-- Rules -->
    <lane *case="1" pointer-leave=|~ControlPanelContext.HandleHoverRuleEntry()|
        layout="content[1236..] content"
        orientation="vertical"
        padding="4,4">
      <paginator has-pagination={HasRuleEntryPagination} curr-page={RuleEntriesPage} />
      <image *if={HasRuleEntryPagination} sprite={@Mods/StardewUI/Sprites/ThinHorizontalDivider} layout="1236px content" margin="0,4,8,0" fit="Stretch"/>
      <scrollable peeking="128" scrollbar-margin="8,0,0,0">
        <lane orientation="vertical">
          <panel *repeat={RuleEntriesFilteredPaginated} vertical-content-alignment="end">
            <lane orientation="horizontal" margin="0,0,0,10">
              <lane *repeat={:this}
                pointer-enter=|~ControlPanelContext.HandleHoverRuleEntry(RIE)|
                pointer-leave=|~ControlPanelContext.SetHoverRule()|
                opacity={Opacity}
                orientation="vertical" margin="6"
                horizontal-content-alignment="middle">
                <panel *if={Active} margin="0,12">
                  <checkbox *if={~ControlPanelContext.IsMainPlayer} is-checked={<>State}/>
                  <panel *!if={~ControlPanelContext.IsMainPlayer} opacity="0.5">
                    <image *if={State} sprite={@Mods/StardewUI/Sprites/CheckboxChecked} />
                    <image *!if={State} sprite={@Mods/StardewUI/Sprites/CheckboxUnchecked} />
                  </panel>
                </panel>
                <spacer *!if={Active} layout="36px 60px" />
                <rule-icon *repeat={:Inputs} />
                <spacer layout={:InputSpacerLayout}/>
                <image sprite={SpinningCaret}
                  layout="36px 36px"
                  margin="18,12,18,12"/>
                <rule-icon *repeat={Outputs} />
                <image *if={:HasOutputOverflow} left-click=|ToggleOverflowOutputs()|
                  sprite={ToggleOverflowSprite}
                  layout="28px 32px"
                  focusable="true"
                  padding="9,-2,9,8"/>
              </lane>
            </lane>
            <image *!if={:LastRow} sprite={@Mods/StardewUI/Sprites/ThinHorizontalDivider} layout="1236px content" margin="-4,0,0,0" fit="Stretch"/>
          </panel>
        </lane>
      </scrollable>
    </lane>

    <!-- Inputs -->
    <lane *case="2" orientation="vertical">
      <lane orientation="horizontal" margin="16,4">
        <image *repeat={QualityStars} sprite={:Sprite} tint={Tint}
          layout="24px 24px"
          margin="4,12"
          focusable="true"
          +hover:scale="1.1"
          +transition:scale="100ms EaseInSine"
          left-click=|ToggleState()|/>
          <paginator has-pagination={HasInputItemsPagination}  curr-page={InputItemsPage} />
      </lane>
      <image sprite={@Mods/StardewUI/Sprites/ThinHorizontalDivider} layout="1236px content" margin="0,0,8,0" fit="Stretch"/>
      <!-- <image *if={HasRuleEntryPagination} sprite={@Mods/StardewUI/Sprites/ThinHorizontalDivider} layout="1236px content" margin="0,2,8,0" fit="Stretch"/> -->
      <scrollable peeking="128" scrollbar-margin="8,0,0,0">
        <grid pointer-leave=|~ControlPanelContext.SetHoverInput()| *case="2" item-layout="length: 76+" horizontal-item-alignment="middle">
          <panel *if={~ControlPanelContext.IsMainPlayer} *repeat={InputItemsFilteredPaginated}
            tooltip={:Tooltip}
            left-click=|ToggleState()|
            pointer-enter=|~ControlPanelContext.SetHoverInput(this)|
            focusable="true">
            <image *repeat={:SpriteLayers} sprite={:Sprite} tint={^Tint} padding={:Padding} layout={:Layout}
              margin="6"
              focusable="true"
              +hover:scale="1.1"
              +transition:scale="100ms EaseInSine"/>
          </panel>
        </grid>
      </scrollable>
    </lane>
  </frame>
</lane>

<template name="tab-label">
  <frame
    background={@mushymato.MachineControlPanel/sprites/cursors:tabBg}
    padding="18,16,18,12"
    focusable="true"
    margin={&margin}
    left-click=|ChangePage(&page)|
    >
    <label text={&text}/>
  </frame>
</template>

<template name="rule-icon">
  <panel tooltip={:Tooltip}
    left-click=|ShowSubItemGrid()|
    right-click=|ShowByproductsGrid()|
    pointer-enter=|~ControlPanelContext.SetHoverRule(this)|
    focusable="true"
    padding="4" >
    <image *repeat={:SpriteLayers} sprite={:Sprite} tint={:Tint} padding={:Padding} layout={:Layout} margin="2" opacity={:^IsMultiOpacity}/>
    <image *if={:IsMulti} sprite={@mushymato.MachineControlPanel/sprites/emojis:note} layout="27px 27px" />
    <image *if={:HasDesc} sprite={@mushymato.MachineControlPanel/sprites/emojis:exclaim} layout="27px 27px" />
    <panel layout="stretch stretch" horizontal-content-alignment="end" vertical-content-alignment="start">
      <image *if={:IsFuel} sprite={@mushymato.MachineControlPanel/sprites/emojis:bolt} layout="27px 27px"/>
      <image *repeat={:EMCByproductReprItem} sprite={:Sprite} tint={:Tint} padding={:Padding} layout={:Layout}/>
    </panel>
    <panel *if={:HasQualityStar} layout="stretch stretch" horizontal-content-alignment="start" vertical-content-alignment="end">
      <image sprite={:QualityStar} layout="24px 24px"/>
    </panel>
    <panel *if={:ShowCount} layout="stretch stretch"  horizontal-content-alignment="end" vertical-content-alignment="end">
      <digits number={:Count} scale="3"/>
    </panel>
  </panel>
</template>

<template name="paginator">
  <lane *if={&has-pagination} orientation="horizontal" margin="6,0">
    <button
      hover-background={@Mods/StardewUI/Sprites/ButtonLight}
      layout="45% content"
      left-click=|PrevPaginatedPage()|
      text={#pagination.prev-page}
    />
    <banner text={&curr-page} layout="10% stretch"/>
    <button
      hover-background={@Mods/StardewUI/Sprites/ButtonLight}
      layout="stretch content"
      left-click=|NextPaginatedPage()|
      text={#pagination.next-page}
    />
  </lane>
</template>