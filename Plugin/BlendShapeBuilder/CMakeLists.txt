file(GLOB sources *.cpp *.h)
set(plugins_dir "${CMAKE_SOURCE_DIR}/../BlendShapeBuilder/Assets/UTJ/BlendShapeBuilder/Plugins/x86_64")
add_plugin(BlendShapeBuilderCore SOURCES ${sources} PLUGINS_DIR ${plugins_dir})

add_dependencies(BlendShapeBuilderCore MeshUtils)
target_link_libraries(BlendShapeBuilderCore MeshUtils ${EXTERNAL_LIBS})
install(TARGETS BlendShapeBuilderCore DESTINATION .)
