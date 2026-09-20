import { Editor } from "@tinymce/tinymce-react";
import "tinymce/tinymce";
import "tinymce/models/dom";
import "tinymce/icons/default";
import "tinymce/themes/silver";
import "tinymce/plugins/autolink";
import "tinymce/plugins/image";
import "tinymce/plugins/link";
import "tinymce/plugins/lists";
import "tinymce/plugins/code";
import "tinymce/plugins/wordcount";
import "tinymce/skins/ui/oxide/skin.min.css";
import "tinymce/skins/content/default/content.min.css";
import { uploadAdminFile } from "../../shared/api/files";

interface TinyEditorProps {
  disabled?: boolean;
  id?: string;
  onChange: (value: string) => void;
  value: string;
}

export function TinyEditor({ disabled = false, id = "tiny-editor", onChange, value }: TinyEditorProps) {
  return (
    <Editor
      disabled={disabled}
      id={id}
      init={{
        branding: false,
        content_css: false,
        content_style:
          "body { color: #20242a; font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; font-size: 16px; line-height: 1.65; margin: 1rem; } a { color: #2563eb; } img { display: block; height: auto; margin: 1rem 0; max-width: 100%; }",
        height: 520,
        image_advtab: false,
        image_dimensions: true,
        image_title: true,
        image_uploadtab: true,
        images_file_types: "jpeg,jpg,png,webp,svg",
        images_upload_handler: async (blobInfo) => {
          const response = await uploadAdminFile(blobInfo.blob(), "content", blobInfo.filename());

          return response.url;
        },
        menubar: false,
        plugins: "autolink link lists image code wordcount",
        skin: false,
        toolbar:
          "undo redo | blocks | bold italic underline | bullist numlist | link unlink | image | removeformat | code",
        toolbar_mode: "sliding"
      }}
      licenseKey="gpl"
      onEditorChange={onChange}
      value={value}
    />
  );
}
